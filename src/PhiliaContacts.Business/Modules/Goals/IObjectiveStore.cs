using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Goals;

public interface IObjectiveStore
{
    Task AddAsync(Objective objective, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Objective>> GetAsync(GoalId? goalId = null, bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<Objective?> GetByIdAsync(ObjectiveId id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Objective objective, CancellationToken cancellationToken = default);
}
