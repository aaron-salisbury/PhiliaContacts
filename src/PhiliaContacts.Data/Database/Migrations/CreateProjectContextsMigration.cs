using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class CreateProjectContextsMigration : Migration
{
    internal override long Version => 3;

    internal override string Description => "Create ProjectContext table";

    protected override async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE ProjectContext (
                Id BLOB NOT NULL PRIMARY KEY CHECK(length(Id) = 16),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                State INTEGER NOT NULL CHECK(State IN (0, 1, 2)),
                IsArchived INTEGER NOT NULL DEFAULT 0 CHECK(IsArchived IN (0, 1))
            );
            """;

        CommandDefinition command = new(sql, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}
