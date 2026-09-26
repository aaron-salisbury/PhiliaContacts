using FluentValidation;
using PhiliaContacts.Business.Modules.Goals;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Business.Modules.LifeDomains;
using PhiliaContacts.Business.Modules.Projects;
using PhiliaContacts.Business.Modules.Relationships;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace PhiliaContacts.Business;

public static class DependencyInjection
{
    /// <summary>
    /// Register internal business-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ICalendarReader, CalendarReader>();
        services.AddScoped<IGoalService, GoalService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddSingleton<IProviderRegistry, ProviderRegistry>();
        services.AddScoped<ILifeDomainService, LifeDomainService>();
        services.AddScoped<IObjectiveService, ObjectiveService>();
        services.AddScoped<IProjectContextService, ProjectContextService>();
        services.AddScoped<IResourceBrowser, ResourceBrowser>();
        services.AddScoped<IRelationshipService, RelationshipService>();
        services.AddScoped<IResourceExistenceService, ResourceExistenceService>();

        return services;
    }
}
