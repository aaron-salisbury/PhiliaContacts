using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Projects;

public interface IProjectContextStore
{
    Task AddAsync(ProjectContext projectContext, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectContext>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    Task<ProjectContext?> GetByIdAsync(ProjectContextId id, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProjectContext projectContext, CancellationToken cancellationToken = default);
}
