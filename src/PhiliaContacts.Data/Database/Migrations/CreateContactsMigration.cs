using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class CreateContactsMigration : Migration
{
    internal override long Version => 1;
    internal override string Description => "Create contact storage";

    protected override Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE Contact (
                Id TEXT NOT NULL PRIMARY KEY,
                GivenName TEXT, MiddleName TEXT, FamilyName TEXT, Nickname TEXT, Prefix TEXT, Suffix TEXT,
                Birthday TEXT, Title TEXT, Organization TEXT, Photo BLOB,
                TwitterUser TEXT, FacebookUser TEXT, LinkedInUser TEXT, Url TEXT, Notes TEXT,
                IsFavorite INTEGER NOT NULL CHECK(IsFavorite IN (0, 1))
            );
            CREATE TABLE ContactValue (
                ContactId TEXT NOT NULL, Kind TEXT NOT NULL CHECK(Kind IN ('email', 'phone')),
                Position INTEGER NOT NULL, Value TEXT NOT NULL, Type TEXT NOT NULL,
                PRIMARY KEY(ContactId, Kind, Position),
                FOREIGN KEY(ContactId) REFERENCES Contact(Id) ON DELETE CASCADE
            );
            CREATE TABLE ContactAddress (
                ContactId TEXT NOT NULL, Position INTEGER NOT NULL, Type TEXT NOT NULL,
                Street TEXT, City TEXT, Region TEXT, PostalCode TEXT, Country TEXT,
                PRIMARY KEY(ContactId, Position),
                FOREIGN KEY(ContactId) REFERENCES Contact(Id) ON DELETE CASCADE
            );
            CREATE INDEX IX_Contact_Name ON Contact(FamilyName, GivenName);
            """;
        return connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken));
    }
}
