using RunnethOverStudio.AppToolkit.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Goals;

public interface IObjectiveService
{
    Task<ProcessResult<Objective, ObjectiveOperationError>> ArchiveAsync(ObjectiveId id, CancellationToken cancellationToken = default);

    Task<ProcessResult<Objective, ObjectiveOperationError>> CreateAsync(GoalId goalId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Objective>> GetAsync(GoalId? goalId = null, bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<ProcessResult<Objective, ObjectiveOperationError>> MoveAsync(ObjectiveId id, GoalId goalId, CancellationToken cancellationToken = default);

    Task<ProcessResult<Objective, ObjectiveOperationError>> RenameAsync(ObjectiveId id, string name, CancellationToken cancellationToken = default);

    Task<ProcessResult<Objective, ObjectiveOperationError>> RestoreAsync(ObjectiveId id, CancellationToken cancellationToken = default);

    Task<ProcessResult<Objective, ObjectiveOperationError>> SetStateAsync(ObjectiveId id, ObjectiveState state, CancellationToken cancellationToken = default);
}
