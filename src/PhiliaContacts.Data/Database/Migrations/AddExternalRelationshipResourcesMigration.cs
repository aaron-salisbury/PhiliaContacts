using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class AddExternalRelationshipResourcesMigration : Migration
{
    internal override long Version => 6;

    internal override string Description => "Support external relationship resources";

    protected override async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            ALTER TABLE Relationship RENAME TO RelationshipLegacy;

            CREATE TABLE Relationship (
                Id BLOB NOT NULL PRIMARY KEY CHECK(length(Id) = 16),
                SourceType TEXT NOT NULL,
                SourceId BLOB NULL CHECK(SourceId IS NULL OR length(SourceId) = 16),
                SourceIntegrationId BLOB NULL CHECK(SourceIntegrationId IS NULL OR length(SourceIntegrationId) = 16),
                SourceExternalId TEXT NULL,
                SourceResourceKind INTEGER NULL,
                RelationshipKindKey TEXT NOT NULL,
                TargetType TEXT NOT NULL,
                TargetId BLOB NULL CHECK(TargetId IS NULL OR length(TargetId) = 16),
                TargetIntegrationId BLOB NULL CHECK(TargetIntegrationId IS NULL OR length(TargetIntegrationId) = 16),
                TargetExternalId TEXT NULL,
                TargetResourceKind INTEGER NULL,
                FOREIGN KEY (RelationshipKindKey) REFERENCES RelationshipKind(Key),
                CHECK (
                    (SourceType = 'external' AND SourceId IS NULL AND SourceIntegrationId IS NOT NULL AND SourceExternalId IS NOT NULL AND SourceResourceKind IS NOT NULL)
                    OR
                    (SourceType <> 'external' AND SourceId IS NOT NULL AND SourceIntegrationId IS NULL AND SourceExternalId IS NULL AND SourceResourceKind IS NULL)
                ),
                CHECK (
                    (TargetType = 'external' AND TargetId IS NULL AND TargetIntegrationId IS NOT NULL AND TargetExternalId IS NOT NULL AND TargetResourceKind IS NOT NULL)
                    OR
                    (TargetType <> 'external' AND TargetId IS NOT NULL AND TargetIntegrationId IS NULL AND TargetExternalId IS NULL AND TargetResourceKind IS NULL)
                )
            );

            INSERT INTO Relationship (
                Id, SourceType, SourceId, RelationshipKindKey, TargetType, TargetId)
            SELECT
                Id, SourceType, SourceId, RelationshipKindKey, TargetType, TargetId
            FROM RelationshipLegacy;

            DROP TABLE RelationshipLegacy;

            CREATE INDEX IX_Relationship_Source
                ON Relationship(SourceType, SourceId, SourceIntegrationId, SourceExternalId, SourceResourceKind);

            CREATE INDEX IX_Relationship_Target
                ON Relationship(TargetType, TargetId, TargetIntegrationId, TargetExternalId, TargetResourceKind);

            CREATE UNIQUE INDEX UX_Relationship_SemanticTriple ON Relationship (
                SourceType,
                ifnull(hex(SourceId), ''),
                ifnull(hex(SourceIntegrationId), ''),
                ifnull(SourceExternalId, ''),
                ifnull(SourceResourceKind, -1),
                RelationshipKindKey,
                TargetType,
                ifnull(hex(TargetId), ''),
                ifnull(hex(TargetIntegrationId), ''),
                ifnull(TargetExternalId, ''),
                ifnull(TargetResourceKind, -1)
            );
            """;

        CommandDefinition command = new(sql, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
