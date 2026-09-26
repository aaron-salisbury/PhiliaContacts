using System;

namespace PhiliaContacts.Business.Modules.Goals;

public readonly record struct GoalId(Guid Value)
{
    public static GoalId New()
    {
        return new GoalId(Guid.NewGuid());
    }
}
