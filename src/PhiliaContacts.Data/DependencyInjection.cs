using PhiliaContacts.Business.Modules.Goals;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Business.Modules.LifeDomains;
using PhiliaContacts.Business.Modules.Projects;
using PhiliaContacts.Business.Modules.Relationships;
using PhiliaContacts.Data.Database;
using PhiliaContacts.Data.Database.Migrations;
using PhiliaContacts.Data.Modules.Goals;
using PhiliaContacts.Data.Modules.Integrations;
using PhiliaContacts.Data.Modules.LifeDomains;
using PhiliaContacts.Data.Modules.Projects;
using PhiliaContacts.Data.Modules.Relationships;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace PhiliaContacts.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Register internal data-tier services.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">Thrown when <paramref name="applicationDataDirectory"/> does not exist.</exception>
    /// <returns>A reference to this instance after the operation has completed.</returns>
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
        services.AddSingleton<PhiliaContactsDatabaseInitializer>(serviceProvider =>
            new PhiliaContactsDatabaseInitializer(
                serviceProvider.GetRequiredService<IPhiliaContactsDatabase>(),
                //TODO: Consider generating the ordered migration registry when maintaining this explicit list becomes burdensome.
                //      Prefer compile-time generation and validation over runtime assembly discovery.
                [
                    new CreateLifeDomainMigration(),
                    new CreateGoalsMigration(),
                    new CreateProjectContextsMigration(),
                    new CreateRelationshipsMigration(),
                    new CreateIntegrationsMigration(),
                    new AddExternalRelationshipResourcesMigration()
                ]));

        services.AddScoped<IGoalStore, SqliteGoalStore>();
        services.AddScoped<IIntegrationStore, SqliteIntegrationStore>();
        services.AddScoped<ILifeDomainStore, SqliteLifeDomainStore>();
        services.AddScoped<IObjectiveStore, SqliteObjectiveStore>();
        services.AddScoped<IProjectContextStore, SqliteProjectContextStore>();
        services.AddScoped<IRelationshipKindStore, SqliteRelationshipKindStore>();
        services.AddScoped<IRelationshipStore, SqliteRelationshipStore>();

        return services;
    }
}
