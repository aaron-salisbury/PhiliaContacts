using RunnethOverStudio.AppToolkit.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

/// <summary>
/// Provider-side contract for the hierarchical resource browsing capability.
/// </summary>
public interface IResourceBrowserProvider : IProvider
{
    Task<ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>> BrowseAsync(
        Integration integration,
        ResourceReference? collection = null,
        CancellationToken cancellationToken = default);
}
