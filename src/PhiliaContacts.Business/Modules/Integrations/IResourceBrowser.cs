using RunnethOverStudio.AppToolkit.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

/// <summary>
/// Browses hierarchical external resources through a configured Integration.
/// </summary>
public interface IResourceBrowser
{
    Task<ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>> BrowseAsync(
        IntegrationId integrationId,
        ResourceReference? collection = null,
        CancellationToken cancellationToken = default);
}
