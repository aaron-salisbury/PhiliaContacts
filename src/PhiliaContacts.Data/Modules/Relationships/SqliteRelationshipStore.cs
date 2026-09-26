using Dapper;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Business.Modules.Relationships;
using PhiliaContacts.Data.Database;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Relationships;

internal sealed class SqliteRelationshipStore : IRelationshipStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteRelationshipStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(relationship);

        ResourceLocatorStorage source = ResourceLocatorMapping.ToStorage(relationship.Source);
        ResourceLocatorStorage target = ResourceLocatorMapping.ToStorage(relationship.Target);

        const string sql = """
            INSERT INTO Relationship (
                Id, SourceType, SourceId, SourceIntegrationId, SourceExternalId, SourceResourceKind,
                RelationshipKindKey,
                TargetType, TargetId, TargetIntegrationId, TargetExternalId, TargetResourceKind)
            VALUES (
                @Id, @SourceType, @SourceId, @SourceIntegrationId, @SourceExternalId, @SourceResourceKind,
                @RelationshipKindKey,
                @TargetType, @TargetId, @TargetIntegrationId, @TargetExternalId, @TargetResourceKind);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = relationship.Id.Value.ToByteArray(),
                SourceType = source.Type,
                SourceId = source.Id,
                SourceIntegrationId = source.IntegrationId,
                SourceExternalId = source.ExternalId,
                SourceResourceKind = source.ResourceKind,
                relationship.RelationshipKindKey,
                TargetType = target.Type,
                TargetId = target.Id,
                TargetIntegrationId = target.IntegrationId,
                TargetExternalId = target.ExternalId,
                TargetResourceKind = target.ResourceKind
            },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(ResourceLocator source, string relationshipKindKey, ResourceLocator target, CancellationToken cancellationToken = default)
    {
        ResourceLocatorStorage sourceStorage = ResourceLocatorMapping.ToStorage(source);
        ResourceLocatorStorage targetStorage = ResourceLocatorMapping.ToStorage(target);

        const string sql = """
            SELECT EXISTS(
                SELECT 1
                FROM Relationship
                WHERE SourceType = @SourceType
                  AND SourceId IS @SourceId
                  AND SourceIntegrationId IS @SourceIntegrationId
                  AND SourceExternalId IS @SourceExternalId
                  AND SourceResourceKind IS @SourceResourceKind
                  AND RelationshipKindKey = @RelationshipKindKey
                  AND TargetType = @TargetType
                  AND TargetId IS @TargetId
                  AND TargetIntegrationId IS @TargetIntegrationId
                  AND TargetExternalId IS @TargetExternalId
                  AND TargetResourceKind IS @TargetResourceKind);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new
            {
                SourceType = sourceStorage.Type,
                SourceId = sourceStorage.Id,
                SourceIntegrationId = sourceStorage.IntegrationId,
                SourceExternalId = sourceStorage.ExternalId,
                SourceResourceKind = sourceStorage.ResourceKind,
                RelationshipKindKey = relationshipKindKey,
                TargetType = targetStorage.Type,
                TargetId = targetStorage.Id,
                TargetIntegrationId = targetStorage.IntegrationId,
                TargetExternalId = targetStorage.ExternalId,
                TargetResourceKind = targetStorage.ResourceKind
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Relationship>> GetAsync(ResourceLocator resource, CancellationToken cancellationToken = default)
    {
        ResourceLocatorStorage storage = ResourceLocatorMapping.ToStorage(resource);

        const string sql = """
            SELECT Id, SourceType, SourceId, SourceIntegrationId, SourceExternalId, SourceResourceKind,
                   RelationshipKindKey,
                   TargetType, TargetId, TargetIntegrationId, TargetExternalId, TargetResourceKind
            FROM Relationship
            WHERE (SourceType = @Type AND SourceId IS @Id AND SourceIntegrationId IS @IntegrationId AND SourceExternalId IS @ExternalId AND SourceResourceKind IS @ResourceKind)
               OR (TargetType = @Type AND TargetId IS @Id AND TargetIntegrationId IS @IntegrationId AND TargetExternalId IS @ExternalId AND TargetResourceKind IS @ResourceKind)
            ORDER BY RelationshipKindKey, Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        IEnumerable<RelationshipRow> rows = await connection.QueryAsync<RelationshipRow>(
            new CommandDefinition(sql, new { storage.Type, storage.Id, storage.IntegrationId, storage.ExternalId, storage.ResourceKind }, cancellationToken: cancellationToken));

        return [.. rows.Select(Map)];
    }

    public async Task<Relationship?> GetByIdAsync(RelationshipId id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, SourceType, SourceId, SourceIntegrationId, SourceExternalId, SourceResourceKind,
                   RelationshipKindKey,
                   TargetType, TargetId, TargetIntegrationId, TargetExternalId, TargetResourceKind
            FROM Relationship
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        RelationshipRow? row = await connection.QuerySingleOrDefaultAsync<RelationshipRow>(
            new CommandDefinition(sql, new { Id = id.Value.ToByteArray() }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task<bool> HasExternalReferencesAsync(IntegrationId integrationId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1
                FROM Relationship
                WHERE SourceIntegrationId = @IntegrationId
                   OR TargetIntegrationId = @IntegrationId);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { IntegrationId = integrationId.Value.ToByteArray() }, cancellationToken: cancellationToken));
    }

    public async Task RemoveAsync(RelationshipId id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Relationship WHERE Id = @Id;";
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id.Value.ToByteArray() }, cancellationToken: cancellationToken));
    }

    private static Relationship Map(RelationshipRow row)
    {
        return new Relationship(
            new RelationshipId(new Guid(row.Id)),
            ResourceLocatorMapping.FromStorage(row.SourceType, row.SourceId, row.SourceIntegrationId, row.SourceExternalId, row.SourceResourceKind),
            row.RelationshipKindKey,
            ResourceLocatorMapping.FromStorage(row.TargetType, row.TargetId, row.TargetIntegrationId, row.TargetExternalId, row.TargetResourceKind));
    }

    private sealed class RelationshipRow
    {
        public byte[] Id { get; init; } = [];

        public string RelationshipKindKey { get; init; } = string.Empty;

        public string? SourceExternalId { get; init; }

        public byte[]? SourceId { get; init; }

        public byte[]? SourceIntegrationId { get; init; }

        public int? SourceResourceKind { get; init; }

        public string SourceType { get; init; } = string.Empty;

        public string? TargetExternalId { get; init; }

        public byte[]? TargetId { get; init; }

        public byte[]? TargetIntegrationId { get; init; }

        public int? TargetResourceKind { get; init; }

        public string TargetType { get; init; } = string.Empty;
    }
}
