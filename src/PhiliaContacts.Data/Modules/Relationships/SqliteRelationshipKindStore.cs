using Dapper;
using PhiliaContacts.Business.Modules.Relationships;
using PhiliaContacts.Data.Database;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Relationships;

internal sealed class SqliteRelationshipKindStore : IRelationshipKindStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteRelationshipKindStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task AddAsync(RelationshipKind relationshipKind, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationshipKind);

        const string sql = """
            INSERT INTO RelationshipKind (Key, DisplayName, InverseDisplayName, Description, IsBuiltIn)
            VALUES (@Key, @DisplayName, @InverseDisplayName, @Description, @IsBuiltIn);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, relationshipKind, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM RelationshipKind WHERE Key = @Key);";
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<RelationshipKind>> GetAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Key, DisplayName, InverseDisplayName, Description, IsBuiltIn
            FROM RelationshipKind
            ORDER BY DisplayName COLLATE NOCASE, Key;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        IEnumerable<RelationshipKindRow> rows = await connection.QueryAsync<RelationshipKindRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return [.. rows.Select(Map)];
    }

    public async Task<RelationshipKind?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Key, DisplayName, InverseDisplayName, Description, IsBuiltIn
            FROM RelationshipKind
            WHERE Key = @Key;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        RelationshipKindRow? row = await connection.QuerySingleOrDefaultAsync<RelationshipKindRow>(
            new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task UpdateAsync(RelationshipKind relationshipKind, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationshipKind);

        const string sql = """
            UPDATE RelationshipKind
            SET DisplayName = @DisplayName,
                InverseDisplayName = @InverseDisplayName,
                Description = @Description
            WHERE Key = @Key;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, relationshipKind, cancellationToken: cancellationToken));
    }

    private static RelationshipKind Map(RelationshipKindRow row)
    {
        return new RelationshipKind(row.Key, row.DisplayName, row.InverseDisplayName, row.Description, row.IsBuiltIn);
    }

    private sealed class RelationshipKindRow
    {
        public string Description { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public string InverseDisplayName { get; init; } = string.Empty;

        public bool IsBuiltIn { get; init; }

        public string Key { get; init; } = string.Empty;
    }
}
