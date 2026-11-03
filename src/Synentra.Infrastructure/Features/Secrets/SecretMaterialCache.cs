using Synentra.Application.Common.Security;
using Synentra.Infrastructure.Configuration.Secrets;

namespace Synentra.Infrastructure.Features.Secrets;

/// <summary>Only immutable material is shared. Provider work executes in the winning caller's own scope.</summary>
public sealed class SecretMaterialCache : IDisposable
{
    private readonly TimeProvider clock;
    private readonly SecretsConfiguration _options;
    private readonly ITimer timer;
    public SecretMaterialCache(SecretsConfiguration options, TimeProvider? time = null)
    {
        options.Validate(); 
        _options = options; 
        clock = time ?? TimeProvider.System;
        var period = TimeSpan.FromSeconds(Math.Min(options.Resolution.Timeout.Seconds, 60));
        timer = clock.CreateTimer(_ => PruneExpired(), null, period, period);
    }

    private void PruneExpired()
    {
        lock (sync) foreach (var key in entries.Where(x => x.Value.Until <= clock.GetUtcNow()).Select(x => x.Key).ToArray()) entries.Remove(key);
    }

    private readonly object sync = new();
    private readonly Dictionary<SecretCacheKey, Entry> entries = [];
    private readonly SemaphoreSlim[] gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    private sealed record Entry(ResolvedCredential Material, DateTimeOffset Until);

    public async Task<ResolvedCredential> ResolveAsync(SecretCacheKey key, Func<CancellationToken, Task<ResolvedCredential>> fetch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _options.Validate();
        if (_options.Cache.Mode == "none" || _options.Cache.OutagePolicy == "require-provider") 
            return await ReadAsync(fetch, ct);

        if (TryGet(key, out var cached)) 
            return cached!;

        var gate = gates[(int)((uint)key.GetHashCode() % (uint)gates.Length)];
        await gate.WaitAsync(ct);
        try
        {
            if (TryGet(key, out cached)) 
                return cached!;

            var material = await ReadAsync(fetch, ct);
            var until = clock.GetUtcNow().AddSeconds(_options.Cache.Ttl.Seconds);
            if (material.ExpiresAt is { } expiry && expiry < until) until = expiry;
            lock (sync)
            {
                foreach (var expired in entries.Where(x => x.Value.Until <= clock.GetUtcNow()).Select(x => x.Key).ToArray()) entries.Remove(expired);
                if (entries.Count >= _options.Cache.MaximumEntries && !entries.ContainsKey(key)) entries.Remove(entries.MinBy(x => x.Value.Until).Key);
                entries[key] = new(material, until);
            }
            return material;
        }
        finally { gate.Release(); }
    }

    private bool TryGet(SecretCacheKey key, out ResolvedCredential? material)
    {
        lock (sync)
        {
            if (entries.TryGetValue(key, out var entry) && entry.Until > clock.GetUtcNow() &&
                (entry.Material.ExpiresAt is null || entry.Material.ExpiresAt > clock.GetUtcNow()))
            { material = entry.Material; return true; }
            entries.Remove(key); material = null; return false;
        }
    }
    private static async Task<ResolvedCredential> ReadAsync(Func<CancellationToken, Task<ResolvedCredential>> fetch, CancellationToken ct)
    {
        var result = await fetch(ct); 
        ct.ThrowIfCancellationRequested(); 
        SecretPayload.Validate(result); 
        return result;
    }

    public void Dispose() { 
        timer.Dispose(); 
        lock (sync) entries.Clear(); 

        foreach (var gate in gates) 
            gate.Dispose(); 
    }
}

