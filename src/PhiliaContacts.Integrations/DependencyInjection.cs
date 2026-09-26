using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Integrations.CalDav;
using PhiliaContacts.Integrations.Credentials;
using PhiliaContacts.Integrations.WebDav;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;

namespace PhiliaContacts.Integrations;

public static class DependencyInjection
{
    /// <summary>
    /// Register internal integrations-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalIntegrationsServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICredentialStore>(_ =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsCredentialStore();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxSecretServiceCredentialStore();
            }

            return new UnsupportedCredentialStore();
        });
        services.AddSingleton<GoogleOAuthAuthorization>();
        services.AddSingleton<CalDavProvider>();
        services.AddSingleton<IProvider>(serviceProvider => serviceProvider.GetRequiredService<CalDavProvider>());
        services.AddSingleton<WebDavProvider>();
        services.AddSingleton<IProvider>(serviceProvider => serviceProvider.GetRequiredService<WebDavProvider>());

        return services;
    }
}
