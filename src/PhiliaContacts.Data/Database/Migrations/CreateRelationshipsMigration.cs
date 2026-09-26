using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class CreateRelationshipsMigration : Migration
{
    internal override long Version => 4;

    internal override string Description => "Create relationship tables";

    protected override async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE RelationshipKind (
                Key TEXT NOT NULL PRIMARY KEY,
                DisplayName TEXT NOT NULL CHECK(length(trim(DisplayName)) > 0),
                InverseDisplayName TEXT NOT NULL CHECK(length(trim(InverseDisplayName)) > 0),
                Description TEXT NOT NULL,
                IsBuiltIn INTEGER NOT NULL DEFAULT 0 CHECK(IsBuiltIn IN (0, 1))
            );

            CREATE TABLE Relationship (
                Id BLOB NOT NULL PRIMARY KEY CHECK(length(Id) = 16),
                SourceType TEXT NOT NULL,
                SourceId BLOB NOT NULL CHECK(length(SourceId) = 16),
                RelationshipKindKey TEXT NOT NULL,
                TargetType TEXT NOT NULL,
                TargetId BLOB NOT NULL CHECK(length(TargetId) = 16),
                FOREIGN KEY (RelationshipKindKey) REFERENCES RelationshipKind(Key),
                UNIQUE (SourceType, SourceId, RelationshipKindKey, TargetType, TargetId)
            );

            CREATE INDEX IX_Relationship_Source ON Relationship(SourceType, SourceId);
            CREATE INDEX IX_Relationship_Target ON Relationship(TargetType, TargetId);

            INSERT INTO RelationshipKind (Key, DisplayName, InverseDisplayName, Description, IsBuiltIn) VALUES
                ('related-to', 'Related to', 'Related to', 'Indicates a general semantic relationship between two resources.', 1),
                ('supports', 'Supports', 'Supported by', 'Indicates that the source supports the target.', 1),
                ('advances', 'Advances', 'Advanced by', 'Indicates that the source advances the target.', 1),
                ('documents', 'Documents', 'Documented by', 'Indicates that the source documents the target.', 1),
                ('implements', 'Implements', 'Implemented by', 'Indicates that the source implements the target.', 1);
            """;

        CommandDefinition command = new(sql, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
