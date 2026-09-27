using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class TrackLegacyImportsMigration : Migration
{
    internal override long Version => 2;
    internal override string Description => "Track legacy source imports";

    protected override Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE LegacyContactImport (
                SourcePath TEXT NOT NULL PRIMARY KEY,
                Sha256 TEXT NOT NULL,
                ContactCount INTEGER NOT NULL,
                ImportedAtUtc TEXT NOT NULL
            );
            """;

        return connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken));
    }
}
