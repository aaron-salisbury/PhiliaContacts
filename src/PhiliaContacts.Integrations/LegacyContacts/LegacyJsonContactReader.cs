using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PhiliaContacts.Integrations.LegacyContacts;

internal sealed class LegacyJsonContactReader : ILegacyContactReader
{
    private static readonly string[] PhoneTypes =
    ["Work", "Cell", "Home", "Voice", "Text", "Fax", "Pager", "Video", "TextPhone", "MainNumber", "BBS", "Modem", "Car", "ISDN", "None"];
    private static readonly string[] EmailTypes = ["Work", "Internet", "Home", "AOL", "Applelink", "IBMMail", "None"];
    private static readonly string[] AddressTypes = ["Work", "Home", "Domestic", "International", "Postal", "Parcel", "None"];

    public IReadOnlyList<Contact> Read(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        LegacyContact[] records = JsonSerializer.Deserialize<LegacyContact[]>(json) ??
            throw new JsonException("The legacy contact file must contain an array.");

        return records.Select(record => Convert(record ?? throw new JsonException("Null contact in legacy data."))).ToArray();
    }

    private static Contact Convert(LegacyContact source)
    {
        return new Contact
        {
            Id = ContactId.New(),
            GivenName = source.GivenName,
            MiddleName = source.MiddleName,
            FamilyName = source.FamilyName,
            Nickname = source.Nickname,
            Prefix = source.Prefix,
            Suffix = source.Suffix,
            Birthday = source.Birthday,
            Title = source.Title,
            Organization = source.Organization,
            Photo = source.Photo,
            TwitterUser = source.TwitterUser,
            FacebookUser = source.FacebookUser,
            LinkedInUser = source.LinkedInUser,
            Url = source.Url,
            Notes = source.Notes,
            IsFavorite = source.IsFavorite,
            EmailAddresses = (source.EmailAddresses ?? []).Select(item =>
                new ContactValue(item?.Email ?? throw new JsonException("Null email entry."), TypeName(item.Type, EmailTypes))).ToArray(),
            PhoneNumbers = (source.PhoneNumbers ?? []).Select(item =>
                new ContactValue(item?.Number ?? throw new JsonException("Null phone entry."), TypeName(item.Type, PhoneTypes))).ToArray(),
            Addresses = HasAddress(source)
                ? [new ContactAddress(TypeName(source.AddressType, AddressTypes), source.Street, source.City, source.State, source.Zip, source.CountryRegion)]
                : []
        };
    }

    private static bool HasAddress(LegacyContact contact) =>
        !string.IsNullOrEmpty(contact.Street) || !string.IsNullOrEmpty(contact.City) ||
        !string.IsNullOrEmpty(contact.State) || !string.IsNullOrEmpty(contact.Zip) ||
        !string.IsNullOrEmpty(contact.CountryRegion);

    private static string TypeName(int value, string[] values) =>
        value >= 0 && value < values.Length ? values[value] : $"legacy:{value}";

    private sealed class LegacyContact
    {
        public LegacyContact() { }
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
        public List<LegacyEmail?>? EmailAddresses { get; set; }
        public List<LegacyPhone?>? PhoneNumbers { get; set; }
        public int AddressType { get; set; } = 6;
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? CountryRegion { get; set; }
    }

    private sealed class LegacyEmail
    {
        public LegacyEmail() { }
        public string? Email { get; set; }
        public int Type { get; set; }
    }

    private sealed class LegacyPhone
    {
        public LegacyPhone() { }
        public string? Number { get; set; }
        public int Type { get; set; }
    }
}
