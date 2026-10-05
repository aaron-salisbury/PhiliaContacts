using Microsoft.Extensions.DependencyInjection;
using PhiliaContacts.Presentation.Desktop.Base.Controls.RibbonControls;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;

namespace PhiliaContacts.Presentation.Desktop;

public static class DependencyInjection
{
    /// <summary>
    /// Register internal presentation-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalPresentationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IRibbonControlFactory, RibbonControlFactory>();

        // View Models and ribbon content.
        foreach (Type assemblyType in typeof(App).Assembly.GetTypes())
        {
            if (assemblyType.IsClass && !assemblyType.IsAbstract)
            {
                if (typeof(BaseViewModel).IsAssignableFrom(assemblyType))
                {
                    services.AddTransient(assemblyType);
                }
                else if (typeof(BaseRibbonControl).IsAssignableFrom(assemblyType))
                {
                    services.AddTransient(assemblyType);
                }
            }
        }

        return services;
    }
}
