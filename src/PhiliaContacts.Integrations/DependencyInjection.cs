using Microsoft.Extensions.DependencyInjection;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Integrations.LegacyContacts;
using System;
using System.IO;

namespace PhiliaContacts.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalIntegrationsServices(this IServiceCollection services, string? applicationDataDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ILegacyContactReader, LegacyJsonContactReader>();
        services.AddSingleton<ILegacyContactFiles>(_ => new LegacyContactFiles(applicationDataDirectory ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhiliaContacts")));
        services.AddScoped<IContactExportService, ContactJsonExportService>();
        services.AddSingleton<IVCardContactService, VCardContactService>();

        return services;
    }
}
