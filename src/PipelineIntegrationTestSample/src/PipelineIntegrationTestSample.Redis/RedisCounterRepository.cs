using PipelineIntegrationTestSample.Abstractions;
using StackExchange.Redis;

namespace PipelineIntegrationTestSample.Redis;

public sealed class RedisCounterRepository(string connectionString) : ICounterRepository
{
    private readonly string _cs = connectionString;
    private ConnectionMultiplexer? _mux;

    private async Task<IDatabase> GetDbAsync()
    {
        _mux ??= await ConnectionMultiplexer.ConnectAsync(_cs);
        return _mux.GetDatabase();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
    }

    public async Task IncrementAsync(string key, long delta = 1, CancellationToken ct = default)
    {
        var db = await GetDbAsync();
        await db.StringIncrementAsync($"counter:{key}", delta);
    }

    public async Task<CounterRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        var db = await GetDbAsync();
        var v = await db.StringGetAsync($"counter:{key}");
        if (!v.HasValue) return null;
        return new CounterRecord(key, (long)v);
    }
}
