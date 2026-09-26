using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class CreateLifeDomainMigration : Migration
{
    internal override long Version => 1;

    internal override string Description => "Create LifeDomain table";

    protected override async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE LifeDomain (
                Id BLOB NOT NULL PRIMARY KEY CHECK(length(Id) = 16),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                IsArchived INTEGER NOT NULL DEFAULT 0 CHECK(IsArchived IN (0, 1))
            );
            """;

        CommandDefinition command = new(sql, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
