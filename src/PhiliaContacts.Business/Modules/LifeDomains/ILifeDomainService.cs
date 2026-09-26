using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.LifeDomains;

public interface ILifeDomainService
{
    Task<bool> ArchiveAsync(LifeDomainId id, CancellationToken cancellationToken = default);

    Task<LifeDomain> CreateAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LifeDomain>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<bool> RenameAsync(LifeDomainId id, string name, CancellationToken cancellationToken = default);

    Task<bool> RestoreAsync(LifeDomainId id, CancellationToken cancellationToken = default);
}
