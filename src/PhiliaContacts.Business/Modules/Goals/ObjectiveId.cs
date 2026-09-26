using System;

namespace PhiliaContacts.Business.Modules.Goals;

public readonly record struct ObjectiveId(Guid Value)
{
    public static ObjectiveId New()
    {
        return new ObjectiveId(Guid.NewGuid());
    }
}
