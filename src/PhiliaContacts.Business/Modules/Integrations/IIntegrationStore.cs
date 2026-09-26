using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

public interface IIntegrationStore
{
    Task AddAsync(Integration integration, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Integration>> GetAsync(CancellationToken cancellationToken = default);

    Task<Integration?> GetByIdAsync(IntegrationId id, CancellationToken cancellationToken = default);

    Task RemoveAsync(IntegrationId id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Integration integration, CancellationToken cancellationToken = default);
}
