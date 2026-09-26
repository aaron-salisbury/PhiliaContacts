using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Projects;

internal sealed class ProjectContextService : IProjectContextService
{
    private readonly IProjectContextStore _projectContextStore;

    public ProjectContextService(IProjectContextStore projectContextStore)
    {
        _projectContextStore = projectContextStore ?? throw new ArgumentNullException(nameof(projectContextStore));
    }

    public Task<ProcessResult<ProjectContext, ProjectContextOperationError>> ArchiveAsync(ProjectContextId id, CancellationToken cancellationToken = default)
    {
        return SetArchivedAsync(id, true, cancellationToken);
    }

    public async Task<ProjectContext> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        ProjectContext projectContext = new(ProjectContextId.New(), NormalizeName(name), ProjectContextState.Active, false);
        await _projectContextStore.AddAsync(projectContext, cancellationToken);
        return projectContext;
    }

    public Task<IReadOnlyList<ProjectContext>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        return _projectContextStore.GetAsync(includeArchived, cancellationToken);
    }

    public async Task<ProcessResult<ProjectContext, ProjectContextOperationError>> RenameAsync(ProjectContextId id, string name, CancellationToken cancellationToken = default)
    {
        ProjectContext? projectContext = await _projectContextStore.GetByIdAsync(id, cancellationToken);

        if (projectContext is null)
        {
            return ProcessResult<ProjectContext, ProjectContextOperationError>.Failure(ProjectContextOperationError.ProjectContextNotFound);
        }

        return await UpdateAsync(projectContext with { Name = NormalizeName(name) }, cancellationToken);
    }

    public Task<ProcessResult<ProjectContext, ProjectContextOperationError>> RestoreAsync(ProjectContextId id, CancellationToken cancellationToken = default)
    {
        return SetArchivedAsync(id, false, cancellationToken);
    }

    public async Task<ProcessResult<ProjectContext, ProjectContextOperationError>> SetStateAsync(ProjectContextId id, ProjectContextState state, CancellationToken cancellationToken = default)
    {
        ProjectContext? projectContext = await _projectContextStore.GetByIdAsync(id, cancellationToken);

        if (projectContext is null)
        {
            return ProcessResult<ProjectContext, ProjectContextOperationError>.Failure(ProjectContextOperationError.ProjectContextNotFound);
        }

        return await UpdateAsync(projectContext with { State = state }, cancellationToken);
    }

    private async Task<ProcessResult<ProjectContext, ProjectContextOperationError>> SetArchivedAsync(ProjectContextId id, bool isArchived, CancellationToken cancellationToken)
    {
        ProjectContext? projectContext = await _projectContextStore.GetByIdAsync(id, cancellationToken);

        if (projectContext is null)
        {
            return ProcessResult<ProjectContext, ProjectContextOperationError>.Failure(ProjectContextOperationError.ProjectContextNotFound);
        }

        return projectContext.IsArchived == isArchived
            ? ProcessResult<ProjectContext, ProjectContextOperationError>.Success(projectContext)
            : await UpdateAsync(projectContext with { IsArchived = isArchived }, cancellationToken);
    }

    private async Task<ProcessResult<ProjectContext, ProjectContextOperationError>> UpdateAsync(ProjectContext projectContext, CancellationToken cancellationToken)
    {
        await _projectContextStore.UpdateAsync(projectContext, cancellationToken);
        return ProcessResult<ProjectContext, ProjectContextOperationError>.Success(projectContext);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}
