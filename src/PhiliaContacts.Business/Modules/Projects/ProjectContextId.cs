using System;

namespace PhiliaContacts.Business.Modules.Projects;

public readonly record struct ProjectContextId(Guid Value)
{
    public static ProjectContextId New()
    {
        return new ProjectContextId(Guid.NewGuid());
    }
}
