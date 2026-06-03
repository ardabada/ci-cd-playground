using System.Data;
using Dapper;
using Npgsql;
using PipelineIntegrationTestSample.Abstractions;

namespace PipelineIntegrationTestSample.Postgres;

public sealed class PostgresCounterRepository(string connectionString) : ICounterRepository
{
    private readonly string _cs = connectionString;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);
        const string sql = """
            create table if not exists counters(
                key text primary key,
                value bigint not null default 0
            );
        """;
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task IncrementAsync(string key, long delta = 1, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);
        const string sql = """
            insert into counters(key, value) values(@key, @delta)
            on conflict (key) do update set value = counters.value + @delta;
        """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { key, delta }, cancellationToken: ct));
    }

    public async Task<CounterRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync(ct);
        const string sql = "select key, value from counters where key = @key;";
        var row = await conn.QuerySingleOrDefaultAsync<(string Key, long Value)>(
            new CommandDefinition(sql, new { key }, cancellationToken: ct));
        return row == default ? null : new CounterRecord(row.Key, row.Value);
    }
}
