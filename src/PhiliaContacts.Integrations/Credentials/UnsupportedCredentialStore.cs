using PhiliaContacts.Business.Modules.Integrations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Integrations.Credentials;

internal sealed class UnsupportedCredentialStore : ICredentialStore
{
    public Task DeleteAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        return Task.FromException(new PlatformNotSupportedException("Protected credential storage is not available on this platform."));
    }

    public Task<CredentialMaterial?> GetAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        return Task.FromException<CredentialMaterial?>(new PlatformNotSupportedException("Protected credential storage is not available on this platform."));
    }

    public Task StoreAsync(CredentialReference reference, CredentialMaterial material, CancellationToken cancellationToken = default)
    {
        return Task.FromException(new PlatformNotSupportedException("Protected credential storage is not available on this platform."));
    }
}
