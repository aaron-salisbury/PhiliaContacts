using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Contacts;

public interface IContactService
{
    Task<IReadOnlyList<Contact>> ListAsync(CancellationToken cancellationToken = default);
    Task<Contact?> GetAsync(ContactId id, CancellationToken cancellationToken = default);
    Task SaveAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(ContactId id, CancellationToken cancellationToken = default);
}

public interface IContactStore
{
    Task<IReadOnlyList<Contact>> ListAsync(CancellationToken cancellationToken = default);
    Task<Contact?> GetAsync(ContactId id, CancellationToken cancellationToken = default);
    Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(ContactId id, CancellationToken cancellationToken = default);
}

// Parsing is an external-format boundary; discovery, backup and import orchestration follow in Phase 2.
public interface ILegacyContactReader
{
    IReadOnlyList<Contact> Read(string json);
}
