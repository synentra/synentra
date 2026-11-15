using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Synentra.Infrastructure.Configuration.Logging;

namespace Synentra.Infrastructure.Logging;

public class LoggerFactory: ILoggerFactory
{
    private readonly IOptions<LoggingConfiguration> options;

    public LoggerFactory(IOptions<LoggingConfiguration> options)
    {
        this.options = options;
    }

    public ILogger CreateLogger()
    {
        var configuration = options.Value;
        var config = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithEnvironmentUserName();

        // Console
        ConfigureConsoleSink(config);

        // File
        ConfigureFileSink(config, configuration.File);

        // Seq
        ConfigureSeqSink(config, configuration.Seq);

        return config.CreateLogger();
    }

    private static void ConfigureConsoleSink(LoggerConfiguration config)
    {
        config.WriteTo.Console(
            restrictedToMinimumLevel: LogEventLevel.Information,
            outputTemplate:
                "[{Timestamp:dd/MMM/yyyy HH:mm:ss.fff} {Level:u3}] [{MachineName}] [{ProcessName}:{ProcessId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        );
    }

    private static void ConfigureFileSink(LoggerConfiguration config, FileLoggingConfiguration? fileLoggingConfig)
    {
        if (fileLoggingConfig == null 
            || !fileLoggingConfig.Enabled 
            || string.IsNullOrWhiteSpace(fileLoggingConfig.LogPath)) 
            return;

        config.WriteTo.File(
            path: fileLoggingConfig.LogPath,
            restrictedToMinimumLevel: LogLevelMapper(fileLoggingConfig.LogLevel),
            outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [Thread:{ThreadId}] " +
                "[Machine:{MachineName}] [Process:{ProcessName}:{ProcessId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingIntervalFromString(fileLoggingConfig.RollingInterval),
            retainedFileCountLimit: fileLoggingConfig.RetainedFileCountLimit ?? 7,
            shared: true
        );
    }

    private static void ConfigureSeqSink(LoggerConfiguration config, SeqLoggingConfiguration? seqLoggingConfig)
    {
        if (seqLoggingConfig == null
            || !seqLoggingConfig.Enabled
            || string.IsNullOrWhiteSpace(seqLoggingConfig.Url)) return;

        config.WriteTo.Seq(
            serverUrl: seqLoggingConfig.Url!,
            apiKey: string.IsNullOrWhiteSpace(seqLoggingConfig.ApiKey) ? null : seqLoggingConfig.ApiKey,
            restrictedToMinimumLevel: LogLevelMapper(seqLoggingConfig.LogLevel)
        );
    }

    public static LogEventLevel LogLevelMapper(string? level) => level?.ToLowerInvariant() switch
    {
        "verbose" or "trace" => LogEventLevel.Verbose,
        "debug" => LogEventLevel.Debug,
        "information" or "info" => LogEventLevel.Information,
        "warning" or "warn" => LogEventLevel.Warning,
        "error" => LogEventLevel.Error,
        "fatal" or "critical" => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };

    public static RollingInterval RollingIntervalFromString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return RollingInterval.Infinite;

        return Enum.TryParse<RollingInterval>(value, true, out var interval)
            ? interval
            : RollingInterval.Day;
    }
}
