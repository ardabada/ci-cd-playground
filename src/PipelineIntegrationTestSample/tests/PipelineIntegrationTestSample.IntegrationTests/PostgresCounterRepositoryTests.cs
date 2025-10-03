using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PipelineIntegrationTestSample.Abstractions;
using PipelineIntegrationTestSample.Postgres;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PipelineIntegrationTestSample.IntegrationTests;

public sealed class PostgresCounterRepositoryTests : IAsyncLifetime
{
    private readonly IConfiguration _cfg = TestConfig.Build();
    private IContainer? _container;
    private string? _cs;

    public async Task InitializeAsync()
    {
        var useExternal = _cfg.GetValue("UseExternal:Postgres", false);
        var externalCs = _cfg.GetValue<string?>("Connections:Postgres");

        if (useExternal && !string.IsNullOrWhiteSpace(externalCs))
        {
            _cs = externalCs;
            return;
        }

        _container = new ContainerBuilder()
            .WithImage("postgres:16")
            .WithName($"it-pg-{Guid.NewGuid():N}")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_DB", "testdb")
            .WithPortBinding(0, 5432)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await _container.StartAsync();

        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(5432);
        _cs = $"Host={host};Port={port};Username=postgres;Password=postgres;Database=testdb;Pooling=true;Timeout=5";
        // optional: probe
        for (var i = 0; i < 20; i++)
        {
            try { await using var _ = new NpgsqlConnection(_cs); await _.OpenAsync(); break; }
            catch { await Task.Delay(500); }
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    [Fact]
    public async Task IncrementAndRead_Works()
    {
        Assert.False(string.IsNullOrWhiteSpace(_cs));
        ICounterRepository repo = new PostgresCounterRepository(_cs!);
        await repo.InitializeAsync();

        await repo.IncrementAsync("foo");
        await repo.IncrementAsync("foo", 5);

        var rec = await repo.GetAsync("foo");
        Assert.NotNull(rec);
        Assert.Equal(6, rec!.Value);
    }
}
