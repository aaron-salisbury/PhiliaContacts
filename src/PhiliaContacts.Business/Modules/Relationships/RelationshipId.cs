using System;

namespace PhiliaContacts.Business.Modules.Relationships;

public readonly record struct RelationshipId(Guid Value)
{
    public static RelationshipId New()
    {
        return new RelationshipId(Guid.NewGuid());
    }
}
