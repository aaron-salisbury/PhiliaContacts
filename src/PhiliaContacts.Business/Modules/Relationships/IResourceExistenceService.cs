using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Relationships;

public interface IResourceExistenceService
{
    Task<bool> ExistsAsync(ResourceLocator resource, CancellationToken cancellationToken = default);
}
