using PhiliaContacts.Business.Modules.Relationships;
using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

internal sealed class IntegrationService : IIntegrationService
{
    private readonly IIntegrationStore _integrationStore;
    private readonly IProviderRegistry _providerRegistry;
    private readonly IRelationshipStore _relationshipStore;

    public IntegrationService(IIntegrationStore integrationStore, IProviderRegistry providerRegistry, IRelationshipStore relationshipStore)
    {
        _integrationStore = integrationStore ?? throw new ArgumentNullException(nameof(integrationStore));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
        _relationshipStore = relationshipStore ?? throw new ArgumentNullException(nameof(relationshipStore));
    }

    public async Task<ProcessResult<IntegrationHealthResult, IntegrationOperationError>> CheckHealthAsync(IntegrationId id, CancellationToken cancellationToken = default)
    {
        Integration? integration = await _integrationStore.GetByIdAsync(id, cancellationToken);

        if (integration is null)
        {
            return ProcessResult<IntegrationHealthResult, IntegrationOperationError>.Failure(IntegrationOperationError.IntegrationNotFound);
        }

        IProvider? provider = _providerRegistry.Find(integration.ProviderKey);

        if (provider is null)
        {
            return ProcessResult<IntegrationHealthResult, IntegrationOperationError>.Failure(IntegrationOperationError.ProviderNotAvailable);
        }

        IntegrationHealthResult health = await provider.CheckHealthAsync(integration, cancellationToken);
        return ProcessResult<IntegrationHealthResult, IntegrationOperationError>.Success(health);
    }

    public async Task<ProcessResult<Integration, IntegrationOperationError>> CreateAsync(ProviderKey providerKey, string name, ProviderConfiguration configuration, CredentialReference? credentialReference = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (_providerRegistry.Find(providerKey) is null)
        {
            return ProcessResult<Integration, IntegrationOperationError>.Failure(IntegrationOperationError.ProviderNotAvailable);
        }

        Integration integration = new(
            IntegrationId.New(),
            providerKey,
            NormalizeName(name),
            IntegrationState.Enabled,
            configuration,
            credentialReference);

        await _integrationStore.AddAsync(integration, cancellationToken);
        return ProcessResult<Integration, IntegrationOperationError>.Success(integration);
    }

    public Task<IReadOnlyList<Integration>> GetAsync(CancellationToken cancellationToken = default)
    {
        return _integrationStore.GetAsync(cancellationToken);
    }

    public async Task<ProcessResult<Integration, IntegrationOperationError>> RemoveAsync(IntegrationId id, CancellationToken cancellationToken = default)
    {
        Integration? integration = await _integrationStore.GetByIdAsync(id, cancellationToken);

        if (integration is null)
        {
            return ProcessResult<Integration, IntegrationOperationError>.Failure(IntegrationOperationError.IntegrationNotFound);
        }

        if (await _relationshipStore.HasExternalReferencesAsync(id, cancellationToken))
        {
            return ProcessResult<Integration, IntegrationOperationError>.Failure(IntegrationOperationError.IntegrationInUse);
        }

        await _integrationStore.RemoveAsync(id, cancellationToken);
        return ProcessResult<Integration, IntegrationOperationError>.Success(integration);
    }

    public async Task<ProcessResult<Integration, IntegrationOperationError>> RenameAsync(IntegrationId id, string name, CancellationToken cancellationToken = default)
    {
        Integration? integration = await _integrationStore.GetByIdAsync(id, cancellationToken);

        if (integration is null)
        {
            return ProcessResult<Integration, IntegrationOperationError>.Failure(IntegrationOperationError.IntegrationNotFound);
        }

        return await UpdateAsync(integration with { Name = NormalizeName(name) }, cancellationToken);
    }

    public async Task<ProcessResult<Integration, IntegrationOperationError>> SetConfigurationAsync(IntegrationId id, ProviderConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Integration? integration = await _integrationStore.GetByIdAsync(id, cancellationToken);

        if (integration is null)
        {
            return ProcessResult<Integration, IntegrationOperationError>.Failure(IntegrationOperationError.IntegrationNotFound);
        }

        return await UpdateAsync(integration with { Configuration = configuration }, cancellationToken);
    }

    public async Task<ProcessResult<Integration, IntegrationOperationError>> SetCredentialReferenceAsync(IntegrationId id, CredentialReference? credentialReference, CancellationToken cancellationToken = default)
    {
        Integration? integration = await _integrationStore.GetByIdAsync(id, cancellationToken);

        if (integration is null)
        {
            return ProcessResult<Integration, IntegrationOperationError>.Failure(IntegrationOperationError.IntegrationNotFound);
        }

        return await UpdateAsync(integration with { CredentialReference = credentialReference }, cancellationToken);
    }

    public async Task<ProcessResult<Integration, IntegrationOperationError>> SetStateAsync(IntegrationId id, IntegrationState state, CancellationToken cancellationToken = default)
    {
        Integration? integration = await _integrationStore.GetByIdAsync(id, cancellationToken);

        if (integration is null)
        {
            return ProcessResult<Integration, IntegrationOperationError>.Failure(IntegrationOperationError.IntegrationNotFound);
        }

        return await UpdateAsync(integration with { State = state }, cancellationToken);
    }

    private async Task<ProcessResult<Integration, IntegrationOperationError>> UpdateAsync(Integration integration, CancellationToken cancellationToken)
    {
        await _integrationStore.UpdateAsync(integration, cancellationToken);
        return ProcessResult<Integration, IntegrationOperationError>.Success(integration);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
