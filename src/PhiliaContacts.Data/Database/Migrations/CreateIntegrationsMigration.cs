using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class CreateIntegrationsMigration : Migration
{
    internal override long Version => 5;

    internal override string Description => "Create Integration table";

    protected override async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE Integration (
                Id BLOB NOT NULL PRIMARY KEY CHECK(length(Id) = 16),
                ProviderKey TEXT NOT NULL CHECK(length(trim(ProviderKey)) > 0),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                State INTEGER NOT NULL CHECK(State IN (0, 1)),
                Configuration TEXT NOT NULL CHECK(length(Configuration) > 0),
                CredentialReference TEXT NULL CHECK(CredentialReference IS NULL OR length(trim(CredentialReference)) > 0)
            );

            CREATE INDEX IX_Integration_ProviderKey ON Integration(ProviderKey);
            """;

        CommandDefinition command = new(sql, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
