using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

internal sealed class AuthorizationService : IAuthorizationService
{
    private readonly IIntegrationStore _integrationStore;
    private readonly IProviderRegistry _providerRegistry;

    public AuthorizationService(IIntegrationStore integrationStore, IProviderRegistry providerRegistry)
    {
        _integrationStore = integrationStore ?? throw new ArgumentNullException(nameof(integrationStore));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
    }

    public async Task<ProcessResult<Integration, AuthorizationError>> AuthorizeAsync(IntegrationId integrationId, CancellationToken cancellationToken = default)
    {
        Integration? integration = await _integrationStore.GetByIdAsync(integrationId, cancellationToken);
        if (integration is null)
        {
            return ProcessResult<Integration, AuthorizationError>.Failure(AuthorizationError.IntegrationNotFound);
        }

        if (integration.State != IntegrationState.Enabled)
        {
            return ProcessResult<Integration, AuthorizationError>.Failure(AuthorizationError.IntegrationDisabled);
        }

        IProvider? provider = _providerRegistry.Find(integration.ProviderKey);
        if (provider is null)
        {
            return ProcessResult<Integration, AuthorizationError>.Failure(AuthorizationError.ProviderNotAvailable);
        }

        if (provider is not IAuthorizationProvider authorizationProvider)
        {
            return ProcessResult<Integration, AuthorizationError>.Failure(AuthorizationError.AuthorizationNotSupported);
        }

        ProcessResult<CredentialReference, AuthorizationError> authorization = await authorizationProvider.AuthorizeAsync(integration, cancellationToken);
        if (!authorization.IsSuccessful)
        {
            return ProcessResult<Integration, AuthorizationError>.Failure(authorization.Error);
        }

        Integration updated = integration with { CredentialReference = authorization.Value };
        await _integrationStore.UpdateAsync(updated, cancellationToken);
        return ProcessResult<Integration, AuthorizationError>.Success(updated);
    }
}
