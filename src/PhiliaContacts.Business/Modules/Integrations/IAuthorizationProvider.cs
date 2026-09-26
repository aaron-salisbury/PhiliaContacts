using RunnethOverStudio.AppToolkit.Core;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

public interface IAuthorizationProvider : IProvider
{
    Task<ProcessResult<CredentialReference, AuthorizationError>> AuthorizeAsync(Integration integration, CancellationToken cancellationToken = default);
}
