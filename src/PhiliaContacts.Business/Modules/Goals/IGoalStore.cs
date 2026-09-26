using PhiliaContacts.Business.Modules.LifeDomains;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Goals;

public interface IGoalStore
{
    Task AddAsync(Goal goal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Goal>> GetAsync(LifeDomainId? lifeDomainId = null, bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<Goal?> GetByIdAsync(GoalId id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Goal goal, CancellationToken cancellationToken = default);
}
