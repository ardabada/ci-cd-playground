using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PipelineIntegrationTestSample.Abstractions;
using PipelineIntegrationTestSample.SqlServer;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PipelineIntegrationTestSample.IntegrationTests;

public sealed class SqlServerCounterRepositoryTests : IAsyncLifetime
{
    private readonly IConfiguration _cfg = TestConfig.Build();
    private IContainer? _container;
    private string? _cs;

    public async Task InitializeAsync()
    {
        var useExternal = _cfg.GetValue("UseExternal:SqlServer", false);
        var externalCs = _cfg.GetValue<string?>("Connections:SqlServer");

        if (useExternal && !string.IsNullOrWhiteSpace(externalCs))
        {
            _cs = externalCs;
            return;
        }

        _container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithName($"it-mssql-{Guid.NewGuid():N}")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", "Your_password123")
            .WithPortBinding(0, 1433)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        await _container.StartAsync();
        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(1433);
        _cs = $"Server={host},{port};User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False;Initial Catalog=master";

        // create test DB
        await using var conn = new SqlConnection(_cs);
        for (var i = 0; i < 30; i++)
        {
            try { await conn.OpenAsync(); break; } catch { await Task.Delay(1000); }
        }
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "if db_id('testdb') is null create database testdb;";
            await cmd.ExecuteNonQueryAsync();
        }
        _cs = $"{_cs};Database=testdb";
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
        ICounterRepository repo = new SqlServerCounterRepository(_cs!);
        await repo.InitializeAsync();
        await repo.IncrementAsync("bar", 2);
        await repo.IncrementAsync("bar", 3);
        var rec = await repo.GetAsync("bar");
        Assert.NotNull(rec);
        Assert.Equal(5, rec!.Value);
    }
}
