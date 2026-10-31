using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class AddContactInterchangeMigration : Migration
{
    internal override long Version => 3;
    internal override string Description => "Store phonetic names and vCard extension properties";

    protected override Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            ALTER TABLE Contact ADD COLUMN PhoneticGivenName TEXT;
            ALTER TABLE Contact ADD COLUMN PhoneticFamilyName TEXT;
            CREATE TABLE ContactVCardProperty (
                ContactId TEXT NOT NULL, Position INTEGER NOT NULL, Content TEXT NOT NULL,
                PRIMARY KEY(ContactId, Position),
                FOREIGN KEY(ContactId) REFERENCES Contact(Id) ON DELETE CASCADE
            );
            """;

        return connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken));
    }
}
