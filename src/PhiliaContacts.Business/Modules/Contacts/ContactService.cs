using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Contacts;

internal sealed class ContactService : IContactService
{
    private readonly IContactStore _store;

    public ContactService(IContactStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public Task<IReadOnlyList<Contact>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _store.ListAsync(cancellationToken);
    }

    public Task<Contact?> GetAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(id, cancellationToken);
    }

    public Task<bool> DeleteAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("A contact ID is required.", nameof(id));
        }

        return _store.DeleteAsync(id, cancellationToken);
    }

    public Task SaveAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        ContactValidation.Validate(contact);

        return _store.UpsertAsync(contact, cancellationToken);
    }
}

internal static class ContactValidation
{
    internal const int MAX_PHOTO_BYTES = 8 * 1024 * 1024;

    internal static void Validate(Contact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        if (contact.Id.Value == Guid.Empty)
        {
            throw new ArgumentException("A contact ID is required.", nameof(contact));
        }

        if (string.IsNullOrWhiteSpace(contact.GivenName) && string.IsNullOrWhiteSpace(contact.FamilyName) && string.IsNullOrWhiteSpace(contact.Nickname))
        {
            throw new ArgumentException("A given name, family name or nickname is required.", nameof(contact));
        }

        bool notAllEntriesAreValid = contact.EmailAddresses is null || contact.PhoneNumbers is null || contact.Addresses is null ||
            contact.EmailAddresses.Any(value => value is null || string.IsNullOrWhiteSpace(value.Value)) ||
            contact.PhoneNumbers.Any(value => value is null || string.IsNullOrWhiteSpace(value.Value)) ||
            contact.Addresses.Any(address => address is null);

        if (notAllEntriesAreValid)
        {
            throw new ArgumentException("Contact collections must contain valid entries.", nameof(contact));
        }

        if (contact.VCardProperties is null || contact.VCardProperties.Any(value => string.IsNullOrWhiteSpace(value) || value.Contains('\r') || value.Contains('\n')))
        {
            throw new ArgumentException("vCard extension properties must be single logical lines.", nameof(contact));
        }

        if (contact.Photo?.Length > MAX_PHOTO_BYTES)
        {
            throw new ArgumentException("Contact photo exceeds the 8 MiB limit.", nameof(contact));
        }
    }
}
