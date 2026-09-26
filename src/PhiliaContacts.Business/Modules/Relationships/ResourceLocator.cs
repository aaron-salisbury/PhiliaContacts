using PhiliaContacts.Business.Modules.Goals;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Business.Modules.LifeDomains;
using PhiliaContacts.Business.Modules.Projects;

namespace PhiliaContacts.Business.Modules.Relationships;

public abstract record ResourceLocator
{
    private ResourceLocator()
    {
    }

    public sealed record LifeDomain(LifeDomainId Id) : ResourceLocator;

    public sealed record Goal(GoalId Id) : ResourceLocator;

    public sealed record Objective(ObjectiveId Id) : ResourceLocator;

    public sealed record ProjectContext(ProjectContextId Id) : ResourceLocator;

    public sealed record External(ResourceReference Reference) : ResourceLocator;
}
