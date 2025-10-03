using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using PipelineIntegrationTestSample.Abstractions;

namespace PipelineIntegrationTestSample.SqlServer;

public sealed class SqlServerCounterRepository(string connectionString) : ICounterRepository
{
    private readonly string _cs = connectionString;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync(ct);
        const string sql = """
            if not exists (select 1 from sys.tables where name = 'counters')
            begin
                create table dbo.counters(
                    [key] nvarchar(200) not null primary key,
                    [value] bigint not null default(0)
                );
            end
        """;
        await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task IncrementAsync(string key, long delta = 1, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync(ct);
        const string sql = """
            merge dbo.counters as target
            using (select @key as [key], @delta as [delta]) as source
            on target.[key] = source.[key]
            when matched then update set [value] = target.[value] + source.[delta]
            when not matched then insert([key],[value]) values(source.[key], source.[delta]);
        """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { key, delta }, cancellationToken: ct));
    }

    public async Task<CounterRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync(ct);
        const string sql = "select [key] as [Key], [value] as [Value] from dbo.counters where [key] = @key;";
        var row = await conn.QuerySingleOrDefaultAsync<(string Key, long Value)>(
            new CommandDefinition(sql, new { key }, cancellationToken: ct));
        return row == default ? null : new CounterRecord(row.Key, row.Value);
    }
}
