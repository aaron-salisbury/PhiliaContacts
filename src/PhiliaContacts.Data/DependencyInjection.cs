using Microsoft.Extensions.DependencyInjection;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Data.Database;
using PhiliaContacts.Data.Database.Migrations;
using PhiliaContacts.Data.Modules.Contacts;
using System;
using System.IO;

namespace PhiliaContacts.Data;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalDataServices(this IServiceCollection services, string applicationDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);

        if (!Directory.Exists(applicationDataDirectory))
        {
            throw new DirectoryNotFoundException($"The application data directory '{applicationDataDirectory}' does not exist.");
        }

        string databasePath = Path.Combine(applicationDataDirectory, "PhiliaContacts.db");
        services.AddSingleton<IPhiliaContactsDatabase>(_ => new PhiliaContactsDatabase(databasePath));
        services.AddSingleton<PhiliaContactsDatabaseInitializer>(provider => new PhiliaContactsDatabaseInitializer(
            provider.GetRequiredService<IPhiliaContactsDatabase>(), [new CreateContactsMigration()]));
        services.AddScoped<IContactStore, SqliteContactStore>();

        return services;
    }
}
