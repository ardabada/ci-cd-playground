namespace PipelineIntegrationTestSample.Abstractions;

public interface ICounterRepository
{
    Task InitializeAsync(CancellationToken ct = default);
    Task IncrementAsync(string key, long delta = 1, CancellationToken ct = default);
    Task<CounterRecord?> GetAsync(string key, CancellationToken ct = default);
}
