using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Synentra.Domain.Governance.Intent;
using Synentra.Domain.Governance.Intent.ValueObjects;
using Synentra.Infrastructure.Configuration.Intent;
using Synentra.Infrastructure.Features.Intent;
using System.Text.Json;

namespace Synentra.Infrastructure.Intent.Providers.InternalBert;

/// <summary>
/// ONNX-based intent provider.
///
/// Reads the Community or Pro model package from the local file system and
/// loads all required assets into memory. No temporary files or network
/// access are used during inference.
/// </summary>
public sealed class InternalOnnxIntentProvider(
    IOptions<IntentConfiguration> options, 
    IModelPackageLoader loader, 
    ILogger<InternalOnnxIntentProvider> logger) : IntentProviderBase, IDisposable
{
    public override string ProviderId => "intent-provider/internal";
    public override IntentModality SupportedModalities => IntentModality.Http | IntentModality.Mcp | IntentModality.FunctionCall | IntentModality.Batch | IntentModality.Stream;

    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private readonly TaskCompletionSource _initializationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly int _maxLength;

    private InferenceSession? _session;
    private BertTokenizer? _tokenizer;
    private string[] _intentLabels = [];

    private bool _disposed;

    /// <summary>
    /// Loads the ONNX model, tokenizer, and intent labels into memory.
    /// Safe to call multiple times or concurrently.
    /// </summary>
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!options.Value.Providers.Internal.Enabled)
            return;

        if (_initializationTcs.Task.IsCompleted)
        {
            await _initializationTcs.Task.WaitAsync(cancellationToken);
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);

        try
        {
            // Another caller may have completed initialization while this
            // caller was waiting for the lock.
            if (_initializationTcs.Task.IsCompleted)
            {
                await _initializationTcs.Task.WaitAsync(cancellationToken);
                return;
            }

            var config = options.Value.Providers.Internal;

            logger.LogInformation("Loading Internal ONNX model with maximum sequence length {MaxLength}.", _maxLength);

            var assets = await loader.LoadAsync(
                config,
                cancellationToken);

            if (assets.OnnxBytes.IsEmpty)
            {
                throw new InvalidOperationException("The model package contains an empty ONNX model.");
            }

            if (assets.VocabLines is null || !assets.VocabLines.Any())
            {
                throw new InvalidOperationException("The model package does not contain a valid tokenizer vocabulary.");
            }

            if (assets.IntentLabels is null || assets.IntentLabels.Length == 0)
            {
                throw new InvalidOperationException("The model package does not contain intent labels.");
            }

            var sessionOptions = new SessionOptions();

            var session = new InferenceSession(
                assets.OnnxBytes.ToArray(),
                sessionOptions);

            var tokenizer = new BertTokenizer(assets.VocabLines);
            var labels = assets.IntentLabels;

            ValidateModelMetadata(session, labels);

            _session = session;
            _tokenizer = tokenizer;
            _intentLabels = labels;

            logger.LogInformation("Internal ONNX model loaded successfully with {LabelCount} labels.", labels.Length);

            _initializationTcs.TrySetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize the Internal ONNX intent provider.");

            _initializationTcs.TrySetException(ex);

            // Initialization failure should propagate so the application
            // does not silently run with a configured but unavailable model.
            throw;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public override async Task<IntentLabel> ClassifyAsync(IntentEvidence evidence, CancellationToken ct)
    {
        ThrowIfDisposed();

        if (!options.Value.Providers.Internal.Enabled)
            return Fallback(IntentStatus.Unsupported);

        await _initializationTcs.Task.WaitAsync(ct);

        ct.ThrowIfCancellationRequested();

        var body = BuildPrompt(evidence);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Fallback(IntentStatus.Unsupported);
        }

        var session = _session ?? throw new InvalidOperationException("Internal ONNX session is not initialized.");
        var tokenizer = _tokenizer ?? throw new InvalidOperationException("Internal ONNX tokenizer is not initialized.");

        var (rawIds, rawMask) = tokenizer.Tokenize(body, _maxLength);

        if (rawIds is null)
        {
            throw new InvalidOperationException("The tokenizer returned null input IDs.");
        }

        if (rawMask is null)
        {
            throw new InvalidOperationException("The tokenizer returned a null attention mask.");
        }

        logger.LogDebug(
            "Tokenized intent input. Input IDs: {InputIdCount}; " +
            "attention mask: {AttentionMaskCount}.",
            rawIds.Length,
            rawMask.Length);

        if (rawIds.Length != rawMask.Length)
        {
            throw new InvalidOperationException($"Tokenizer output length mismatch. Input IDs: {rawIds.Length}; attention mask: {rawMask.Length}.");
        }

        var sequenceLength = Math.Min(
            rawIds.Length,
            _maxLength);

        if (sequenceLength <= 0)
        {
            throw new InvalidOperationException("The tokenizer produced an empty token sequence.");
        }

        var inputIds = TruncateToLength(
            rawIds,
            sequenceLength);

        var attentionMask = TruncateToLength(
            rawMask,
            sequenceLength);

        var tokenTypeIds = new long[sequenceLength];
        var dimensions = new[]
        {
            1,
            sequenceLength
        };

        var availableInputs =
            new Dictionary<string, DenseTensor<long>>(
                StringComparer.Ordinal)
            {
                ["input_ids"] = new(
                    inputIds,
                    dimensions),

                ["attention_mask"] = new(
                    attentionMask,
                    dimensions),

                ["token_type_ids"] = new(
                    tokenTypeIds,
                    dimensions)
            };

        var inputs = CreateModelInputs(
            session,
            availableInputs);

        logger.LogDebug(
            "Running Internal ONNX inference with inputs: {Inputs}.",
            string.Join(", ", inputs.Select(input => input.Name)));

        try
        {
            using var results = session.Run(inputs);

            var output = results.FirstOrDefault() ?? throw new InvalidOperationException("The ONNX model returned no outputs.");

            var logits = output.AsTensor<float>().ToArray();

            ValidateLogits(logits);

            var probabilities = Softmax(logits);
            var maximumProbability = probabilities.Max();
            var maximumIndex = Array.IndexOf(
                probabilities,
                maximumProbability);

            if (maximumIndex < 0 ||
                maximumIndex >= _intentLabels.Length)
            {
                throw new InvalidOperationException($"The predicted label index {maximumIndex} is outside the labels range 0-{_intentLabels.Length - 1}.");
            }

            var intent = _intentLabels[maximumIndex];
            var confidence = probabilities[maximumIndex];

            logger.LogDebug(
                "Internal ONNX inference completed. Intent: {Intent}; " +
                "confidence: {Confidence:F4}.",
                intent,
                confidence);

            var content = JsonSerializer.Serialize(new
            {
                intent,
                confidence
            }, JsonOptions);

            return Parse(content);
        }
        catch (OnnxRuntimeException ex)
        {
            logger.LogError(
                ex,
                "ONNX Runtime failed during intent inference. " +
                "Model inputs: {ModelInputs}; supplied inputs: {SuppliedInputs}.",
                FormatInputMetadata(session.InputMetadata),
                string.Join(", ", inputs.Select(input => input.Name)));

            throw;
        }
        catch (NullReferenceException ex)
        {
            // Some ONNX Runtime input/native-runtime mismatches can surface
            // as NullReferenceException from RunImpl.
            logger.LogError(
                ex,
                "ONNX Runtime encountered an invalid internal value during inference. " +
                "Verify model input names, tensor types, dimensions, and ONNX Runtime " +
                "managed/native package versions. Model inputs: {ModelInputs}; " +
                "supplied inputs: {SuppliedInputs}.",
                FormatInputMetadata(session.InputMetadata),
                string.Join(", ", inputs.Select(input => input.Name)));

            throw new InvalidOperationException(
                "Internal ONNX inference failed. Verify the model input metadata " +
                "and ensure all Microsoft.ML.OnnxRuntime packages use the same version.",
                ex);
        }
    }

    private IReadOnlyCollection<NamedOnnxValue> CreateModelInputs(
        InferenceSession session,
        IReadOnlyDictionary<string, DenseTensor<long>> availableInputs)
    {
        var modelMetadata = session.InputMetadata;

        if (modelMetadata.Count == 0)
        {
            throw new InvalidOperationException("The ONNX model declares no inputs.");
        }

        var inputs = new List<NamedOnnxValue>(
            modelMetadata.Count);

        foreach (var (inputName, inputMetadata) in modelMetadata)
        {
            if (!availableInputs.TryGetValue(
                    inputName,
                    out var tensor))
            {
                throw new InvalidOperationException(
                    $"The ONNX model requires input '{inputName}', but the " +
                    $"provider cannot create it. Model inputs: " +
                    $"{string.Join(", ", modelMetadata.Keys)}.");
            }

            if (inputMetadata.ElementType != typeof(long))
            {
                throw new InvalidOperationException(
                    $"ONNX input '{inputName}' expects element type " +
                    $"'{inputMetadata.ElementType}', but the provider supplies " +
                    $"'System.Int64'.");
            }

            ValidateInputDimensions(
                inputName,
                inputMetadata.Dimensions,
                tensor.Dimensions);

            var input = NamedOnnxValue.CreateFromTensor(
                inputName,
                tensor);

            if (input is null)
            {
                throw new InvalidOperationException($"Failed to create ONNX value for input '{inputName}'.");
            }

            inputs.Add(input);
        }

        if (inputs.Count != modelMetadata.Count)
        {
            throw new InvalidOperationException(
                $"ONNX model input mismatch. Expected {modelMetadata.Count} " +
                $"inputs but created {inputs.Count}. Expected inputs: " +
                $"{string.Join(", ", modelMetadata.Keys)}. Created inputs: " +
                $"{string.Join(", ", inputs.Select(input => input.Name))}.");
        }

        return inputs;
    }

    private static void ValidateInputDimensions(
        string inputName,
        IReadOnlyList<int> expectedDimensions,
        ReadOnlySpan<int> actualDimensions)
    {
        if (expectedDimensions.Count != actualDimensions.Length)
        {
            throw new InvalidOperationException(
                $"ONNX input '{inputName}' expects {expectedDimensions.Count} " +
                $"dimensions but received {actualDimensions.Length}. Expected: " +
                $"[{string.Join(",", expectedDimensions)}]; actual: " +
                $"[{string.Join(",", actualDimensions.ToArray())}].");
        }

        for (var index = 0;
             index < expectedDimensions.Count;
             index++)
        {
            var expected = expectedDimensions[index];
            var actual = actualDimensions[index];

            // Negative dimensions represent dynamic dimensions.
            if (expected >= 0 && expected != actual)
            {
                throw new InvalidOperationException(
                    $"ONNX input '{inputName}' has an invalid dimension at " +
                    $"index {index}. Expected: " +
                    $"[{string.Join(",", expectedDimensions)}]; actual: " +
                    $"[{string.Join(",", actualDimensions.ToArray())}].");
            }
        }
    }

    private static void ValidateModelMetadata(
        InferenceSession session,
        IReadOnlyCollection<string> labels)
    {
        if (session.InputMetadata.Count == 0)
        {
            throw new InvalidOperationException("The ONNX model does not declare any inputs.");
        }

        if (!session.InputMetadata.ContainsKey("input_ids"))
        {
            throw new InvalidOperationException("The ONNX model does not declare the required 'input_ids' input.");
        }

        if (!session.InputMetadata.ContainsKey("attention_mask"))
        {
            throw new InvalidOperationException("The ONNX model does not declare the required 'attention_mask' input.");
        }

        ValidateDynamicSequenceDimension(
            session,
            "input_ids");

        ValidateDynamicSequenceDimension(
            session,
            "attention_mask");

        if (session.OutputMetadata.Count == 0)
        {
            throw new InvalidOperationException("The ONNX model does not declare any outputs.");
        }

        if (labels.Count == 0)
        {
            throw new InvalidOperationException("No intent labels were loaded.");
        }
    }

    private static void ValidateDynamicSequenceDimension(
        InferenceSession session,
        string inputName)
    {
        var dimensions = session.InputMetadata[inputName].Dimensions;

        if (dimensions.Length < 2)
        {
            throw new InvalidOperationException(
                $"Internal ONNX model input '{inputName}' must expose batch and " +
                $"sequence dimensions, but declares [{string.Join(",", dimensions)}].");
        }

        var sequenceDimension = dimensions[1];

        if (sequenceDimension >= 0)
        {
            throw new InvalidOperationException(
                "Internal ONNX model uses a fixed sequence dimension of " +
                $"{sequenceDimension} for input '{inputName}'. Dynamic " +
                "sequence-length inference requires the model to be exported " +
                "with a dynamic sequence dimension.");
        }
    }

    private void ValidateLogits(float[] logits)
    {
        if (logits.Length == 0)
        {
            throw new InvalidOperationException("The ONNX model returned an empty logits tensor.");
        }

        if (logits.Length != _intentLabels.Length)
        {
            throw new InvalidOperationException($"The ONNX model returned {logits.Length} logits, but {_intentLabels.Length} intent labels were loaded.");
        }

        if (logits.Any(value =>
                float.IsNaN(value) ||
                float.IsInfinity(value)))
        {
            throw new InvalidOperationException("The ONNX model returned non-finite logits.");
        }
    }

    private static float[] Softmax(float[] logits)
    {
        var maximum = logits.Max();

        var exponentials = logits
            .Select(value =>
                (float)Math.Exp(value - maximum))
            .ToArray();

        var sum = exponentials.Sum();

        if (sum <= 0 ||
            float.IsNaN(sum) ||
            float.IsInfinity(sum))
        {
            throw new InvalidOperationException("Unable to calculate probabilities from the ONNX logits.");
        }

        return exponentials
            .Select(value => value / sum)
            .ToArray();
    }

    private static long[] TruncateToLength(long[] values, int maximumLength)
    {
        if (values.Length <= maximumLength)
            return values;

        return values[..maximumLength];
    }

    private static string FormatInputMetadata(IReadOnlyDictionary<string, NodeMetadata> metadata)
    {
        return string.Join(
            ", ",
            metadata.Select(entry =>
                $"{entry.Key}:{entry.Value.ElementType}" +
                $"[{string.Join(",", entry.Value.Dimensions)}]"));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _session?.Dispose();
        _session = null;
        _tokenizer = null;
        _intentLabels = [];

        _initializationLock.Dispose();
    }
}