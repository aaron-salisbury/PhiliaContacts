using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Goals;

internal sealed class ObjectiveService : IObjectiveService
{
    private readonly IGoalStore _goalStore;
    private readonly IObjectiveStore _objectiveStore;

    public ObjectiveService(IGoalStore goalStore, IObjectiveStore objectiveStore)
    {
        _goalStore = goalStore ?? throw new ArgumentNullException(nameof(goalStore));
        _objectiveStore = objectiveStore ?? throw new ArgumentNullException(nameof(objectiveStore));
    }

    public Task<ProcessResult<Objective, ObjectiveOperationError>> ArchiveAsync(ObjectiveId id, CancellationToken cancellationToken = default)
    {
        return SetArchivedAsync(id, true, cancellationToken);
    }

    public async Task<ProcessResult<Objective, ObjectiveOperationError>> CreateAsync(GoalId goalId, string name, CancellationToken cancellationToken = default)
    {
        if (await _goalStore.GetByIdAsync(goalId, cancellationToken) is null)
        {
            return ProcessResult<Objective, ObjectiveOperationError>.Failure(ObjectiveOperationError.GoalNotFound);
        }

        Objective objective = new(ObjectiveId.New(), goalId, NormalizeName(name), ObjectiveState.Open, false);
        await _objectiveStore.AddAsync(objective, cancellationToken);

        return ProcessResult<Objective, ObjectiveOperationError>.Success(objective);
    }

    public Task<IReadOnlyList<Objective>> GetAsync(GoalId? goalId = null, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        return _objectiveStore.GetAsync(goalId, includeArchived, cancellationToken);
    }

    public async Task<ProcessResult<Objective, ObjectiveOperationError>> MoveAsync(ObjectiveId id, GoalId goalId, CancellationToken cancellationToken = default)
    {
        Objective? objective = await _objectiveStore.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            return ProcessResult<Objective, ObjectiveOperationError>.Failure(ObjectiveOperationError.ObjectiveNotFound);
        }

        if (await _goalStore.GetByIdAsync(goalId, cancellationToken) is null)
        {
            return ProcessResult<Objective, ObjectiveOperationError>.Failure(ObjectiveOperationError.GoalNotFound);
        }

        return await UpdateAsync(objective with { GoalId = goalId }, cancellationToken);
    }

    public async Task<ProcessResult<Objective, ObjectiveOperationError>> RenameAsync(ObjectiveId id, string name, CancellationToken cancellationToken = default)
    {
        Objective? objective = await _objectiveStore.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            return ProcessResult<Objective, ObjectiveOperationError>.Failure(ObjectiveOperationError.ObjectiveNotFound);
        }

        return await UpdateAsync(objective with { Name = NormalizeName(name) }, cancellationToken);
    }

    public Task<ProcessResult<Objective, ObjectiveOperationError>> RestoreAsync(ObjectiveId id, CancellationToken cancellationToken = default)
    {
        return SetArchivedAsync(id, false, cancellationToken);
    }

    public async Task<ProcessResult<Objective, ObjectiveOperationError>> SetStateAsync(ObjectiveId id, ObjectiveState state, CancellationToken cancellationToken = default)
    {
        Objective? objective = await _objectiveStore.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            return ProcessResult<Objective, ObjectiveOperationError>.Failure(ObjectiveOperationError.ObjectiveNotFound);
        }

        return await UpdateAsync(objective with { State = state }, cancellationToken);
    }

    private async Task<ProcessResult<Objective, ObjectiveOperationError>> SetArchivedAsync(ObjectiveId id, bool isArchived, CancellationToken cancellationToken)
    {
        Objective? objective = await _objectiveStore.GetByIdAsync(id, cancellationToken);

        if (objective is null)
        {
            return ProcessResult<Objective, ObjectiveOperationError>.Failure(ObjectiveOperationError.ObjectiveNotFound);
        }

        return objective.IsArchived == isArchived
            ? ProcessResult<Objective, ObjectiveOperationError>.Success(objective)
            : await UpdateAsync(objective with { IsArchived = isArchived }, cancellationToken);
    }

    private async Task<ProcessResult<Objective, ObjectiveOperationError>> UpdateAsync(Objective objective, CancellationToken cancellationToken)
    {
        await _objectiveStore.UpdateAsync(objective, cancellationToken);
        return ProcessResult<Objective, ObjectiveOperationError>.Success(objective);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
