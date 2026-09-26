using RunnethOverStudio.AppToolkit.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Projects;

public interface IProjectContextService
{
    Task<ProcessResult<ProjectContext, ProjectContextOperationError>> ArchiveAsync(ProjectContextId id, CancellationToken cancellationToken = default);

    Task<ProjectContext> CreateAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectContext>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<ProcessResult<ProjectContext, ProjectContextOperationError>> RenameAsync(ProjectContextId id, string name, CancellationToken cancellationToken = default);

    Task<ProcessResult<ProjectContext, ProjectContextOperationError>> RestoreAsync(ProjectContextId id, CancellationToken cancellationToken = default);

    Task<ProcessResult<ProjectContext, ProjectContextOperationError>> SetStateAsync(ProjectContextId id, ProjectContextState state, CancellationToken cancellationToken = default);
}
