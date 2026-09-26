using PhiliaContacts.Business.Modules.Goals;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Business.Modules.LifeDomains;
using PhiliaContacts.Business.Modules.Projects;
using PhiliaContacts.Business.Modules.Relationships;
using System;

namespace PhiliaContacts.Data.Modules.Relationships;

internal static class ResourceLocatorMapping
{
    internal static ResourceLocator FromStorage(string type, byte[]? id, byte[]? integrationId, string? externalId, int? resourceKind)
    {
        if (type == "external")
        {
            if (integrationId is null || externalId is null || resourceKind is null)
            {
                throw new InvalidOperationException("External resource locator data is incomplete.");
            }

            ResourceReference reference = new(
                new IntegrationId(new Guid(integrationId)),
                externalId,
                (ResourceKind)resourceKind.Value);

            return new ResourceLocator.External(reference);
        }

        if (id is null)
        {
            throw new InvalidOperationException($"Resource locator '{type}' does not contain an identifier.");
        }

        Guid value = new(id);

        return type switch
        {
            "life-domain" => new ResourceLocator.LifeDomain(new LifeDomainId(value)),
            "goal" => new ResourceLocator.Goal(new GoalId(value)),
            "objective" => new ResourceLocator.Objective(new ObjectiveId(value)),
            "project-context" => new ResourceLocator.ProjectContext(new ProjectContextId(value)),
            _ => throw new InvalidOperationException($"Unknown resource locator type '{type}'.")
        };
    }

    internal static ResourceLocatorStorage ToStorage(ResourceLocator resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource switch
        {
            ResourceLocator.External external => new ResourceLocatorStorage(
                "external",
                null,
                external.Reference.IntegrationId.Value.ToByteArray(),
                external.Reference.ExternalId,
                (int)external.Reference.Kind),
            ResourceLocator.LifeDomain lifeDomain => PhiliaContactsResource("life-domain", lifeDomain.Id.Value),
            ResourceLocator.Goal goal => PhiliaContactsResource("goal", goal.Id.Value),
            ResourceLocator.Objective objective => PhiliaContactsResource("objective", objective.Id.Value),
            ResourceLocator.ProjectContext projectContext => PhiliaContactsResource("project-context", projectContext.Id.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
    }

    private static ResourceLocatorStorage PhiliaContactsResource(string type, Guid id)
    {
        return new ResourceLocatorStorage(type, id.ToByteArray(), null, null, null);
    }
}

internal sealed record ResourceLocatorStorage(
    string Type,
    byte[]? Id,
    byte[]? IntegrationId,
    string? ExternalId,
    int? ResourceKind);
