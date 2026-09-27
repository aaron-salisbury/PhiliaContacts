using Microsoft.Extensions.DependencyInjection;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Integrations.LegacyContacts;
using System;

namespace PhiliaContacts.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalIntegrationsServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ILegacyContactReader, LegacyJsonContactReader>();

        return services;
    }
}
