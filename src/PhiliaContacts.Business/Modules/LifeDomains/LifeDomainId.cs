using System;

namespace PhiliaContacts.Business.Modules.LifeDomains;

public readonly record struct LifeDomainId(Guid Value)
{
    public static LifeDomainId New()
    {
        return new LifeDomainId(Guid.NewGuid());
    }
}
