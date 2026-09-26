using Dapper;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Data.Database;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Integrations;

internal sealed class SqliteIntegrationStore : IIntegrationStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteIntegrationStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task AddAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integration);

        const string sql = """
            INSERT INTO Integration (Id, ProviderKey, Name, State, Configuration, CredentialReference)
            VALUES (@Id, @ProviderKey, @Name, @State, @Configuration, @CredentialReference);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, integration, cancellationToken));
    }

    public async Task<IReadOnlyList<Integration>> GetAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ProviderKey, Name, State, Configuration, CredentialReference
            FROM Integration
            ORDER BY Name COLLATE NOCASE, Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        IEnumerable<IntegrationRow> rows = await connection.QueryAsync<IntegrationRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return [.. rows.Select(Map)];
    }

    public async Task<Integration?> GetByIdAsync(IntegrationId id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, ProviderKey, Name, State, Configuration, CredentialReference
            FROM Integration
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        IntegrationRow? row = await connection.QuerySingleOrDefaultAsync<IntegrationRow>(
            new CommandDefinition(sql, new { Id = id.Value.ToByteArray() }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task RemoveAsync(IntegrationId id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Integration WHERE Id = @Id;";

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id.Value.ToByteArray() }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integration);

        const string sql = """
            UPDATE Integration
            SET ProviderKey = @ProviderKey,
                Name = @Name,
                State = @State,
                Configuration = @Configuration,
                CredentialReference = @CredentialReference
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, integration, cancellationToken));
    }

    private static CommandDefinition CreateCommand(string sql, Integration integration, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            sql,
            new
            {
                Id = integration.Id.Value.ToByteArray(),
                ProviderKey = integration.ProviderKey.Value,
                integration.Name,
                State = (int)integration.State,
                Configuration = integration.Configuration.Json,
                CredentialReference = integration.CredentialReference?.Value
            },
            cancellationToken: cancellationToken);
    }

    private static Integration Map(IntegrationRow row)
    {
        return new Integration(
            new IntegrationId(new Guid(row.Id)),
            new ProviderKey(row.ProviderKey),
            row.Name,
            (IntegrationState)row.State,
            new ProviderConfiguration(row.Configuration),
            row.CredentialReference is null ? null : new CredentialReference(row.CredentialReference));
    }

    private sealed class IntegrationRow
    {
        public string? CredentialReference { get; init; }

        public string Configuration { get; init; } = string.Empty;

        public byte[] Id { get; init; } = [];

        public string Name { get; init; } = string.Empty;

        public string ProviderKey { get; init; } = string.Empty;

        public int State { get; init; }
    }
}
