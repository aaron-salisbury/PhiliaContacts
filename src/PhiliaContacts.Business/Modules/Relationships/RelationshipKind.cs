namespace PhiliaContacts.Business.Modules.Relationships;

public sealed record RelationshipKind(string Key, string DisplayName, string InverseDisplayName, string Description, bool IsBuiltIn);
