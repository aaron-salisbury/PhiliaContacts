using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.LifeDomains;

public interface ILifeDomainStore
{
    Task AddAsync(LifeDomain lifeDomain, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LifeDomain>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<LifeDomain?> GetByIdAsync(LifeDomainId id, CancellationToken cancellationToken = default);

    Task UpdateAsync(LifeDomain lifeDomain, CancellationToken cancellationToken = default);
}
