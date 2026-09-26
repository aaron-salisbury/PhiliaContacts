using RunnethOverStudio.AppToolkit.Core;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

public interface IAuthorizationService
{
    Task<ProcessResult<Integration, AuthorizationError>> AuthorizeAsync(IntegrationId integrationId, CancellationToken cancellationToken = default);
}
