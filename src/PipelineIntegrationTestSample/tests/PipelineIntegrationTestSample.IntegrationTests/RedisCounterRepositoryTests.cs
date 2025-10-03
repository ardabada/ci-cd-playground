using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using PipelineIntegrationTestSample.Abstractions;
using PipelineIntegrationTestSample.Redis;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PipelineIntegrationTestSample.IntegrationTests;

public sealed class RedisCounterRepositoryTests : IAsyncLifetime
{
    private readonly IConfiguration _cfg = TestConfig.Build();
    private IContainer? _container;
    private string? _cs;

    public async Task InitializeAsync()
    {
        var useExternal = _cfg.GetValue("UseExternal:Redis", false);
        var externalCs = _cfg.GetValue<string?>("Connections:Redis");

        if (useExternal && !string.IsNullOrWhiteSpace(externalCs))
        {
            _cs = externalCs;
            return;
        }

        _container = new ContainerBuilder()
            .WithImage("redis:7")
            .WithName($"it-redis-{Guid.NewGuid():N}")
            .WithPortBinding(0, 6379)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379))
            .Build();

        await _container.StartAsync();
        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(6379);
        _cs = $"{host}:{port}";
        // probe
        for (var i = 0; i < 20; i++)
        {
            try { var mux = await ConnectionMultiplexer.ConnectAsync(_cs); mux.Dispose(); break; }
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
        ICounterRepository repo = new RedisCounterRepository(_cs!);
        await repo.InitializeAsync();
        await repo.IncrementAsync("baz", 10);
        var rec = await repo.GetAsync("baz");
        Assert.NotNull(rec);
        Assert.Equal(10, rec!.Value);
    }
}
