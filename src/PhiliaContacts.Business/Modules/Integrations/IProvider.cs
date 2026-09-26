using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

/// <summary>
/// Describes an integration provider implementation available to PhiliaContacts.
/// </summary>
public interface IProvider
{
    IReadOnlyCollection<CapabilityKey> Capabilities { get; }

    string DisplayName { get; }

    ProviderKey Key { get; }

    Task<IntegrationHealthResult> CheckHealthAsync(Integration integration, CancellationToken cancellationToken = default);
}
