namespace PhiliaContacts.Business.Modules.Relationships;

public sealed record CreateRelationshipRequest(ResourceLocator Source, string RelationshipKindKey, ResourceLocator Target);
