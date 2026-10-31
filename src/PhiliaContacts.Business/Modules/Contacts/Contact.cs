using System;
using System.Collections.Generic;

namespace PhiliaContacts.Business.Modules.Contacts;

public readonly record struct ContactId(Guid Value)
{
    public static ContactId New()
    {
        return new(Guid.NewGuid());
    }
}

public sealed record ContactValue(string Value, string Type);

public sealed record ContactAddress(string Type, string? Street, string? City, string? Region, string? PostalCode, string? Country);

// A contact is an application-facing snapshot. Data rows and UI editing state live in their own layers.
public sealed record Contact
{
    public ContactId Id { get; init; }
    public string? GivenName { get; init; }
    public string? MiddleName { get; init; }
    public string? FamilyName { get; init; }
    public string? PhoneticGivenName { get; init; }
    public string? PhoneticFamilyName { get; init; }
    public string? Nickname { get; init; }
    public string? Prefix { get; init; }
    public string? Suffix { get; init; }
    public string? Birthday { get; init; }
    public string? Title { get; init; }
    public string? Organization { get; init; }
    public byte[]? Photo { get; init; }
    public string? TwitterUser { get; init; }
    public string? FacebookUser { get; init; }
    public string? LinkedInUser { get; init; }
    public string? Url { get; init; }
    public string? Notes { get; init; }
    public bool IsFavorite { get; init; }
    public IReadOnlyList<ContactValue> EmailAddresses { get; init; } = [];
    public IReadOnlyList<ContactValue> PhoneNumbers { get; init; } = [];
    public IReadOnlyList<ContactAddress> Addresses { get; init; } = [];
    public IReadOnlyList<string> VCardProperties { get; init; } = [];

    public string DisplayName => !string.IsNullOrWhiteSpace(Nickname)
        ? Nickname
        : string.Join(" ", new[] { GivenName, FamilyName }.WhereNotBlank());
}
