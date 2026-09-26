using PhiliaContacts.Business.Modules.Goals;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Business.Modules.LifeDomains;
using PhiliaContacts.Business.Modules.Projects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Relationships;

internal sealed class ResourceExistenceService : IResourceExistenceService
{
    private readonly IGoalStore _goalStore;
    private readonly IIntegrationStore _integrationStore;
    private readonly ILifeDomainStore _lifeDomainStore;
    private readonly IObjectiveStore _objectiveStore;
    private readonly IProjectContextStore _projectContextStore;

    public ResourceExistenceService(IGoalStore goalStore, IIntegrationStore integrationStore, ILifeDomainStore lifeDomainStore, IObjectiveStore objectiveStore, IProjectContextStore projectContextStore)
    {
        _goalStore = goalStore ?? throw new ArgumentNullException(nameof(goalStore));
        _integrationStore = integrationStore ?? throw new ArgumentNullException(nameof(integrationStore));
        _lifeDomainStore = lifeDomainStore ?? throw new ArgumentNullException(nameof(lifeDomainStore));
        _objectiveStore = objectiveStore ?? throw new ArgumentNullException(nameof(objectiveStore));
        _projectContextStore = projectContextStore ?? throw new ArgumentNullException(nameof(projectContextStore));
    }

    public async Task<bool> ExistsAsync(ResourceLocator resource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource switch
        {
            ResourceLocator.External external => await _integrationStore.GetByIdAsync(external.Reference.IntegrationId, cancellationToken) is not null,
            ResourceLocator.Goal goal => await _goalStore.GetByIdAsync(goal.Id, cancellationToken) is not null,
            ResourceLocator.LifeDomain lifeDomain => await _lifeDomainStore.GetByIdAsync(lifeDomain.Id, cancellationToken) is not null,
            ResourceLocator.Objective objective => await _objectiveStore.GetByIdAsync(objective.Id, cancellationToken) is not null,
            ResourceLocator.ProjectContext projectContext => await _projectContextStore.GetByIdAsync(projectContext.Id, cancellationToken) is not null,
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
    }
}
