using Dapper;
using Microsoft.Data.Sqlite;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Contacts;

internal sealed class SqliteContactStore : IContactStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteContactStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<IReadOnlyList<Contact>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        IEnumerable<ContactRow> rows = await connection.QueryAsync<ContactRow>(new CommandDefinition(
            "SELECT * FROM Contact ORDER BY IsFavorite DESC, FamilyName COLLATE NOCASE, GivenName COLLATE NOCASE, Id", cancellationToken: cancellationToken));

        IEnumerable<ValueRow> values = await connection.QueryAsync<ValueRow>(new CommandDefinition(
            "SELECT * FROM ContactValue ORDER BY ContactId, Kind, Position", cancellationToken: cancellationToken));

        IEnumerable<AddressRow> addresses = await connection.QueryAsync<AddressRow>(new CommandDefinition(
            "SELECT * FROM ContactAddress ORDER BY ContactId, Position", cancellationToken: cancellationToken));

        ILookup<string, ValueRow> groupedValues = values.ToLookup(value => value.ContactId);
        ILookup<string, AddressRow> groupedAddresses = addresses.ToLookup(address => address.ContactId);

        return [.. rows.Select(row => Map(row, groupedValues[row.Id], groupedAddresses[row.Id]))];
    }

    public async Task<Contact?> GetAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        string key = id.Value.ToString("D");

        ContactRow? row = await connection.QuerySingleOrDefaultAsync<ContactRow>(new CommandDefinition(
            "SELECT * FROM Contact WHERE Id = @key", new { key }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        IEnumerable<ValueRow> values = await connection.QueryAsync<ValueRow>(new CommandDefinition(
            "SELECT * FROM ContactValue WHERE ContactId = @key ORDER BY Kind, Position", new { key }, cancellationToken: cancellationToken));

        IEnumerable<AddressRow> addresses = await connection.QueryAsync<AddressRow>(new CommandDefinition(
            "SELECT * FROM ContactAddress WHERE ContactId = @key ORDER BY Position", new { key }, cancellationToken: cancellationToken));

        return Map(row, values, addresses);
    }

    public async Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction();

        string key = contact.Id.Value.ToString("D");

        const string sql = """
            INSERT INTO Contact (Id, GivenName, MiddleName, FamilyName, Nickname, Prefix, Suffix, Birthday, Title,
                Organization, Photo, TwitterUser, FacebookUser, LinkedInUser, Url, Notes, IsFavorite)
            VALUES (@Id, @GivenName, @MiddleName, @FamilyName, @Nickname, @Prefix, @Suffix, @Birthday, @Title,
                @Organization, @Photo, @TwitterUser, @FacebookUser, @LinkedInUser, @Url, @Notes, @IsFavorite)
            ON CONFLICT(Id) DO UPDATE SET
                GivenName=excluded.GivenName, MiddleName=excluded.MiddleName, FamilyName=excluded.FamilyName,
                Nickname=excluded.Nickname, Prefix=excluded.Prefix, Suffix=excluded.Suffix, Birthday=excluded.Birthday,
                Title=excluded.Title, Organization=excluded.Organization, Photo=excluded.Photo,
                TwitterUser=excluded.TwitterUser, FacebookUser=excluded.FacebookUser, LinkedInUser=excluded.LinkedInUser,
                Url=excluded.Url, Notes=excluded.Notes, IsFavorite=excluded.IsFavorite;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = key, contact.GivenName, contact.MiddleName, contact.FamilyName, contact.Nickname, contact.Prefix,
            contact.Suffix, contact.Birthday, contact.Title, contact.Organization, contact.Photo, contact.TwitterUser,
            contact.FacebookUser, contact.LinkedInUser, contact.Url, contact.Notes, contact.IsFavorite
        }, transaction, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM ContactValue WHERE ContactId = @key; DELETE FROM ContactAddress WHERE ContactId = @key", new { key }, transaction, cancellationToken: cancellationToken));

        for (int i = 0; i < contact.EmailAddresses.Count; i++)
        {
            await InsertValueAsync(connection, transaction, key, "email", i, contact.EmailAddresses[i], cancellationToken);
        }

        for (int i = 0; i < contact.PhoneNumbers.Count; i++)
        {
            await InsertValueAsync(connection, transaction, key, "phone", i, contact.PhoneNumbers[i], cancellationToken);
        }

        for (int i = 0; i < contact.Addresses.Count; i++)
        {
            ContactAddress address = contact.Addresses[i];

            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO ContactAddress (ContactId, Position, Type, Street, City, Region, PostalCode, Country) VALUES (@ContactId, @Position, @Type, @Street, @City, @Region, @PostalCode, @Country)",
                new { ContactId = key, Position = i, address.Type, address.Street, address.City, address.Region, address.PostalCode, address.Country },
                transaction, cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        int count = await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Contact WHERE Id = @Id", new { Id = id.Value.ToString("D") }, cancellationToken: cancellationToken));
        return count != 0;
    }

    private static Task<int> InsertValueAsync(SqliteConnection connection, SqliteTransaction transaction, string id, string kind, int position, ContactValue value, CancellationToken cancellationToken)
    {
        return connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO ContactValue (ContactId, Kind, Position, Value, Type) VALUES (@ContactId, @Kind, @Position, @Value, @Type)",
            new { ContactId = id, Kind = kind, Position = position, value.Value, value.Type }, transaction, cancellationToken: cancellationToken));
    }

    private static Contact Map(ContactRow row, IEnumerable<ValueRow> values, IEnumerable<AddressRow> addresses)
    {
        return new()
        {
            Id = new ContactId(Guid.Parse(row.Id)),
            GivenName = row.GivenName,
            MiddleName = row.MiddleName,
            FamilyName = row.FamilyName,
            Nickname = row.Nickname,
            Prefix = row.Prefix,
            Suffix = row.Suffix,
            Birthday = row.Birthday,
            Title = row.Title,
            Organization = row.Organization,
            Photo = row.Photo,
            TwitterUser = row.TwitterUser,
            FacebookUser = row.FacebookUser,
            LinkedInUser = row.LinkedInUser,
            Url = row.Url,
            Notes = row.Notes,
            IsFavorite = row.IsFavorite,
            EmailAddresses = [.. values.Where(value => value.Kind == "email").Select(value => new ContactValue(value.Value, value.Type))],
            PhoneNumbers = [.. values.Where(value => value.Kind == "phone").Select(value => new ContactValue(value.Value, value.Type))],
            Addresses = [.. addresses.Select(address => new ContactAddress(address.Type, address.Street, address.City, address.Region, address.PostalCode, address.Country))]
        };
    }

    private sealed class ContactRow
    {
        public string Id { get; set; } = "";
        public string? GivenName { get; set; }
        public string? MiddleName { get; set; }
        public string? FamilyName { get; set; }
        public string? Nickname { get; set; }
        public string? Prefix { get; set; }
        public string? Suffix { get; set; }
        public string? Birthday { get; set; }
        public string? Title { get; set; }
        public string? Organization { get; set; }
        public byte[]? Photo { get; set; }
        public string? TwitterUser { get; set; }
        public string? FacebookUser { get; set; }
        public string? LinkedInUser { get; set; }
        public string? Url { get; set; }
        public string? Notes { get; set; }
        public bool IsFavorite { get; set; }
    }

    private sealed class ValueRow
    {
        public string ContactId { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Value { get; set; } = "";
        public string Type { get; set; } = "";
    }

    private sealed class AddressRow
    {
        public string ContactId { get; set; } = "";
        public string Type { get; set; } = "";
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }
}
