using System.Collections.Generic;

namespace PhiliaContacts.Business.Modules.Relationships;

public static class BuiltInRelationshipKinds
{
    public static IReadOnlyList<RelationshipKind> All { get; } =
    [
        new("related-to", "Related to", "Related to", "Indicates a general semantic relationship between two resources.", true),
        new("supports", "Supports", "Supported by", "Indicates that the source supports the target.", true),
        new("advances", "Advances", "Advanced by", "Indicates that the source advances the target.", true),
        new("documents", "Documents", "Documented by", "Indicates that the source documents the target.", true),
        new("implements", "Implements", "Implemented by", "Indicates that the source implements the target.", true)
    ];
}
