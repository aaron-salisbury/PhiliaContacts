using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

/// <summary>
/// Stores opaque credential material outside PhiliaContacts's ordinary application persistence.
/// </summary>
public interface ICredentialStore
{
    Task DeleteAsync(CredentialReference reference, CancellationToken cancellationToken = default);

    Task<CredentialMaterial?> GetAsync(CredentialReference reference, CancellationToken cancellationToken = default);

    Task StoreAsync(CredentialReference reference, CredentialMaterial material, CancellationToken cancellationToken = default);
}
