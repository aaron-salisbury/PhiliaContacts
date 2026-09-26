using Dapper;
using PhiliaContacts.Business.Modules.LifeDomains;
using PhiliaContacts.Data.Database;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.LifeDomains;

internal sealed class SqliteLifeDomainStore : ILifeDomainStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteLifeDomainStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task AddAsync(LifeDomain lifeDomain, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lifeDomain);

        const string sql = """
            INSERT INTO LifeDomain (Id, Name, IsArchived)
            VALUES (@Id, @Name, @IsArchived);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        CommandDefinition command = new(
            sql,
            new
            {
                Id = lifeDomain.Id.Value.ToByteArray(),
                lifeDomain.Name,
                lifeDomain.IsArchived
            },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<IReadOnlyList<LifeDomain>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, IsArchived
            FROM LifeDomain
            WHERE @IncludeArchived = 1 OR IsArchived = 0
            ORDER BY Name COLLATE NOCASE, Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        CommandDefinition command = new(
            sql,
            new { IncludeArchived = includeArchived },
            cancellationToken: cancellationToken);

        IEnumerable<LifeDomainRow> rows = await connection.QueryAsync<LifeDomainRow>(command);
        return [.. rows.Select(Map)];
    }

    public async Task<LifeDomain?> GetByIdAsync(LifeDomainId id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, IsArchived
            FROM LifeDomain
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        CommandDefinition command = new(
            sql,
            new { Id = id.Value.ToByteArray() },
            cancellationToken: cancellationToken);

        LifeDomainRow? row = await connection.QuerySingleOrDefaultAsync<LifeDomainRow>(command);
        return row is null ? null : Map(row);
    }

    public async Task UpdateAsync(LifeDomain lifeDomain, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lifeDomain);

        const string sql = """
            UPDATE LifeDomain
            SET Name = @Name,
                IsArchived = @IsArchived
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        CommandDefinition command = new(
            sql,
            new
            {
                Id = lifeDomain.Id.Value.ToByteArray(),
                lifeDomain.Name,
                lifeDomain.IsArchived
            },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    private static LifeDomain Map(LifeDomainRow row)
    {
        return new LifeDomain(
            new LifeDomainId(new Guid(row.Id)),
            row.Name,
            row.IsArchived);
    }

    private sealed class LifeDomainRow
    {
        public byte[] Id { get; init; } = [];

        public bool IsArchived { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
