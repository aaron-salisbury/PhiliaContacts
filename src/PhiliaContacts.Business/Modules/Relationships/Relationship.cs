namespace PhiliaContacts.Business.Modules.Relationships;

public sealed record Relationship(RelationshipId Id, ResourceLocator Source, string RelationshipKindKey, ResourceLocator Target);
