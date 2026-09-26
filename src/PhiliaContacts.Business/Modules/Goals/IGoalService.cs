using PhiliaContacts.Business.Modules.LifeDomains;
using RunnethOverStudio.AppToolkit.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Goals;

public interface IGoalService
{
    Task<ProcessResult<Goal, GoalOperationError>> ArchiveAsync(GoalId id, CancellationToken cancellationToken = default);

    Task<ProcessResult<Goal, GoalOperationError>> CreateAsync(LifeDomainId lifeDomainId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Goal>> GetAsync(LifeDomainId? lifeDomainId = null, bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<ProcessResult<Goal, GoalOperationError>> MoveAsync(GoalId id, LifeDomainId lifeDomainId, CancellationToken cancellationToken = default);

    Task<ProcessResult<Goal, GoalOperationError>> RenameAsync(GoalId id, string name, CancellationToken cancellationToken = default);

    Task<ProcessResult<Goal, GoalOperationError>> RestoreAsync(GoalId id, CancellationToken cancellationToken = default);

    Task<ProcessResult<Goal, GoalOperationError>> SetStateAsync(GoalId id, GoalState state, CancellationToken cancellationToken = default);
}
