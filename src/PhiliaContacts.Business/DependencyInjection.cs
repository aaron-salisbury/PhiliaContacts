using Microsoft.Extensions.DependencyInjection;
using PhiliaContacts.Business.Modules.Contacts;
using System;

namespace PhiliaContacts.Business;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IContactService, ContactService>();

        return services;
    }
}
