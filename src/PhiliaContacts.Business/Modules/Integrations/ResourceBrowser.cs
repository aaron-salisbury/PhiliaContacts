using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

internal sealed class ResourceBrowser : IResourceBrowser
{
    private readonly IIntegrationStore _integrationStore;
    private readonly IProviderRegistry _providerRegistry;

    public ResourceBrowser(IIntegrationStore integrationStore, IProviderRegistry providerRegistry)
    {
        _integrationStore = integrationStore ?? throw new ArgumentNullException(nameof(integrationStore));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
    }

    public async Task<ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>> BrowseAsync(IntegrationId integrationId, ResourceReference? collection = null, CancellationToken cancellationToken = default)
    {
        Integration? integration = await _integrationStore.GetByIdAsync(integrationId, cancellationToken);

        if (integration is null)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.IntegrationNotFound);
        }

        if (integration.State != IntegrationState.Enabled)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.IntegrationDisabled);
        }

        if (collection is not null && collection.IntegrationId != integrationId)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.InvalidResource);
        }

        IProvider? provider = _providerRegistry.Find(integration.ProviderKey);

        if (provider is null)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.ProviderNotAvailable);
        }

        if (provider is not IResourceBrowserProvider browserProvider)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.CapabilityNotSupported);
        }

        return await browserProvider.BrowseAsync(integration, collection, cancellationToken);
    }
}
