using PhiliaContacts.Business.Modules.LifeDomains;
using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Goals;

internal sealed class GoalService : IGoalService
{
    private readonly IGoalStore _goalStore;
    private readonly ILifeDomainStore _lifeDomainStore;

    public GoalService(IGoalStore goalStore, ILifeDomainStore lifeDomainStore)
    {
        _goalStore = goalStore ?? throw new ArgumentNullException(nameof(goalStore));
        _lifeDomainStore = lifeDomainStore ?? throw new ArgumentNullException(nameof(lifeDomainStore));
    }

    public Task<ProcessResult<Goal, GoalOperationError>> ArchiveAsync(GoalId id, CancellationToken cancellationToken = default)
    {
        return SetArchivedAsync(id, true, cancellationToken);
    }

    public async Task<ProcessResult<Goal, GoalOperationError>> CreateAsync(LifeDomainId lifeDomainId, string name, CancellationToken cancellationToken = default)
    {
        if (await _lifeDomainStore.GetByIdAsync(lifeDomainId, cancellationToken) is null)
        {
            return ProcessResult<Goal, GoalOperationError>.Failure(GoalOperationError.LifeDomainNotFound);
        }

        Goal goal = new(GoalId.New(), lifeDomainId, NormalizeName(name), GoalState.Open, false);
        await _goalStore.AddAsync(goal, cancellationToken);

        return ProcessResult<Goal, GoalOperationError>.Success(goal);
    }

    public Task<IReadOnlyList<Goal>> GetAsync(LifeDomainId? lifeDomainId = null, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        return _goalStore.GetAsync(lifeDomainId, includeArchived, cancellationToken);
    }

    public async Task<ProcessResult<Goal, GoalOperationError>> MoveAsync(GoalId id, LifeDomainId lifeDomainId, CancellationToken cancellationToken = default)
    {
        Goal? goal = await _goalStore.GetByIdAsync(id, cancellationToken);

        if (goal is null)
        {
            return ProcessResult<Goal, GoalOperationError>.Failure(GoalOperationError.GoalNotFound);
        }

        if (await _lifeDomainStore.GetByIdAsync(lifeDomainId, cancellationToken) is null)
        {
            return ProcessResult<Goal, GoalOperationError>.Failure(GoalOperationError.LifeDomainNotFound);
        }

        return await UpdateAsync(goal with { LifeDomainId = lifeDomainId }, cancellationToken);
    }

    public async Task<ProcessResult<Goal, GoalOperationError>> RenameAsync(GoalId id, string name, CancellationToken cancellationToken = default)
    {
        Goal? goal = await _goalStore.GetByIdAsync(id, cancellationToken);

        if (goal is null)
        {
            return ProcessResult<Goal, GoalOperationError>.Failure(GoalOperationError.GoalNotFound);
        }

        return await UpdateAsync(goal with { Name = NormalizeName(name) }, cancellationToken);
    }

    public Task<ProcessResult<Goal, GoalOperationError>> RestoreAsync(GoalId id, CancellationToken cancellationToken = default)
    {
        return SetArchivedAsync(id, false, cancellationToken);
    }

    public async Task<ProcessResult<Goal, GoalOperationError>> SetStateAsync(GoalId id, GoalState state, CancellationToken cancellationToken = default)
    {
        Goal? goal = await _goalStore.GetByIdAsync(id, cancellationToken);

        if (goal is null)
        {
            return ProcessResult<Goal, GoalOperationError>.Failure(GoalOperationError.GoalNotFound);
        }

        return await UpdateAsync(goal with { State = state }, cancellationToken);
    }

    private async Task<ProcessResult<Goal, GoalOperationError>> SetArchivedAsync(GoalId id, bool isArchived, CancellationToken cancellationToken)
    {
        Goal? goal = await _goalStore.GetByIdAsync(id, cancellationToken);

        if (goal is null)
        {
            return ProcessResult<Goal, GoalOperationError>.Failure(GoalOperationError.GoalNotFound);
        }

        return goal.IsArchived == isArchived
            ? ProcessResult<Goal, GoalOperationError>.Success(goal)
            : await UpdateAsync(goal with { IsArchived = isArchived }, cancellationToken);
    }

    private async Task<ProcessResult<Goal, GoalOperationError>> UpdateAsync(Goal goal, CancellationToken cancellationToken)
    {
        await _goalStore.UpdateAsync(goal, cancellationToken);
        return ProcessResult<Goal, GoalOperationError>.Success(goal);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
