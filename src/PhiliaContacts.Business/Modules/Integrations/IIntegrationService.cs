using RunnethOverStudio.AppToolkit.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

public interface IIntegrationService
{
    Task<ProcessResult<IntegrationHealthResult, IntegrationOperationError>> CheckHealthAsync(IntegrationId id, CancellationToken cancellationToken = default);

    Task<ProcessResult<Integration, IntegrationOperationError>> CreateAsync(ProviderKey providerKey, string name, ProviderConfiguration configuration, CredentialReference? credentialReference = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Integration>> GetAsync(CancellationToken cancellationToken = default);

    Task<ProcessResult<Integration, IntegrationOperationError>> RemoveAsync(IntegrationId id, CancellationToken cancellationToken = default);

    Task<ProcessResult<Integration, IntegrationOperationError>> RenameAsync(IntegrationId id, string name, CancellationToken cancellationToken = default);

    Task<ProcessResult<Integration, IntegrationOperationError>> SetConfigurationAsync(IntegrationId id, ProviderConfiguration configuration, CancellationToken cancellationToken = default);

    Task<ProcessResult<Integration, IntegrationOperationError>> SetCredentialReferenceAsync(IntegrationId id, CredentialReference? credentialReference, CancellationToken cancellationToken = default);

    Task<ProcessResult<Integration, IntegrationOperationError>> SetStateAsync(IntegrationId id, IntegrationState state, CancellationToken cancellationToken = default);
}
