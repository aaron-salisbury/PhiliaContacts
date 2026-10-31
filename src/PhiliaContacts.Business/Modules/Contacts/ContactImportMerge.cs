using System;
using System.Collections.Generic;
using System.Linq;

namespace PhiliaContacts.Business.Modules.Contacts;

/// <summary>
/// Combines an imported card with the stored contact it identifies (same <see cref="ContactId"/>).
/// An import can only add or replace information: a field the card leaves empty, or that the importer
/// had to skip (such as a remote photo), keeps its stored value instead of being erased. Repeatable entries
/// are combined: stored entries are kept and the card's entries that are not already present are appended.
/// Retained vCard properties are replaced per property name, so stored extensions the card lacks survive.
/// </summary>
public static class ContactImportMerge
{
    /// <summary>
    /// Resolves a batch of imported contacts against the stored ones and returns the contacts to save.
    /// An imported contact matches a stored one by <see cref="ContactId"/>, or else by an equal retained <c>UID</c>
    /// property (covering contacts imported before IDs were derived from UIDs). Matches are merged rather than
    /// replaced, and imported contacts that resolve to the same ID are merged into each other in order.
    /// </summary>
    public static IReadOnlyList<Contact> MergeIntoStored(IReadOnlyList<Contact> stored, IReadOnlyList<Contact> incoming)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(incoming);

        Dictionary<ContactId, Contact> byId = stored.ToDictionary(contact => contact.Id);
        Dictionary<string, ContactId> byUid = [];
        foreach (Contact contact in stored)
        {
            if (Uid(contact) is { } uid)
            {
                byUid.TryAdd(uid, contact.Id);
            }
        }

        Dictionary<ContactId, Contact> merged = [];
        foreach (Contact original in incoming)
        {
            Contact contact = !byId.ContainsKey(original.Id) && Uid(original) is { } uid && byUid.TryGetValue(uid, out ContactId storedId)
                ? original with { Id = storedId }
                : original;

            merged[contact.Id] = merged.TryGetValue(contact.Id, out Contact? earlier) || byId.TryGetValue(contact.Id, out earlier)
                ? Merge(earlier, contact)
                : contact;
        }

        return [.. merged.Values];
    }

    private static string? Uid(Contact contact)
    {
        string? line = contact.VCardProperties.FirstOrDefault(property => PropertyName(property).Equals("UID", StringComparison.OrdinalIgnoreCase));
        string? value = line?[(line.IndexOf(':') + 1)..].Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static Contact Merge(Contact stored, Contact incoming)
    {
        ArgumentNullException.ThrowIfNull(stored);
        ArgumentNullException.ThrowIfNull(incoming);

        if (stored.Id != incoming.Id)
        {
            throw new ArgumentException("Only contacts with the same ID can be merged.", nameof(incoming));
        }

        return stored with
        {
            GivenName = Pick(incoming.GivenName, stored.GivenName),
            MiddleName = Pick(incoming.MiddleName, stored.MiddleName),
            FamilyName = Pick(incoming.FamilyName, stored.FamilyName),
            PhoneticGivenName = Pick(incoming.PhoneticGivenName, stored.PhoneticGivenName),
            PhoneticFamilyName = Pick(incoming.PhoneticFamilyName, stored.PhoneticFamilyName),
            Nickname = Pick(incoming.Nickname, stored.Nickname),
            Prefix = Pick(incoming.Prefix, stored.Prefix),
            Suffix = Pick(incoming.Suffix, stored.Suffix),
            Birthday = Pick(incoming.Birthday, stored.Birthday),
            Title = Pick(incoming.Title, stored.Title),
            Organization = Pick(incoming.Organization, stored.Organization),
            TwitterUser = Pick(incoming.TwitterUser, stored.TwitterUser),
            FacebookUser = Pick(incoming.FacebookUser, stored.FacebookUser),
            LinkedInUser = Pick(incoming.LinkedInUser, stored.LinkedInUser),
            Url = Pick(incoming.Url, stored.Url),
            Notes = Pick(incoming.Notes, stored.Notes),
            Photo = incoming.Photo is { Length: > 0 } ? incoming.Photo : stored.Photo,
            IsFavorite = incoming.IsFavorite || stored.IsFavorite,
            EmailAddresses = Union(stored.EmailAddresses, incoming.EmailAddresses, value => value.Value.Trim().ToUpperInvariant()),
            PhoneNumbers = Union(stored.PhoneNumbers, incoming.PhoneNumbers, value => new string([.. value.Value.Where(char.IsLetterOrDigit)])),
            Addresses = Union(stored.Addresses, incoming.Addresses, address => string.Join('|',
                new[] { address.Street, address.City, address.Region, address.PostalCode, address.Country }.Select(part => part?.Trim().ToUpperInvariant()))),
            VCardProperties = MergeProperties(stored.VCardProperties, incoming.VCardProperties)
        };
    }

    private static string? Pick(string? incoming, string? stored)
    {
        return string.IsNullOrWhiteSpace(incoming) ? stored : incoming;
    }

    private static IReadOnlyList<T> Union<T>(IReadOnlyList<T> stored, IReadOnlyList<T> incoming, Func<T, string> key)
    {
        HashSet<string> present = [.. stored.Select(key)];
        return [.. stored, .. incoming.Where(entry => present.Add(key(entry)))];
    }

    // Lines are "[group.]NAME[;params]:value". Incoming lines replace stored lines with the same group and property name,
    // so e.g. an incoming item1.X-ABLabel replaces only the stored item1.X-ABLabel, not item2's.
    private static IReadOnlyList<string> MergeProperties(IReadOnlyList<string> stored, IReadOnlyList<string> incoming)
    {
        HashSet<string> replaced = new(incoming.Select(GroupAndName), StringComparer.OrdinalIgnoreCase);
        return [.. stored.Where(line => !replaced.Contains(GroupAndName(line))), .. incoming];
    }

    private static string GroupAndName(string line)
    {
        int colon = line.IndexOf(':');
        string head = colon < 0 ? line : line[..colon];
        return head.Split(';')[0];
    }

    private static string PropertyName(string line)
    {
        return GroupAndName(line).Split('.').Last();
    }
}
