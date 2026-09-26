using System;

namespace PhiliaContacts.Business.Modules.Integrations;

public readonly record struct IntegrationId(Guid Value)
{
    public static IntegrationId New()
    {
        return new IntegrationId(Guid.NewGuid());
    }
}
