using Avalonia;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Data.Database;
using PhiliaContacts.Presentation.Desktop;
using Serilog;
using System;
using System.Threading.Tasks;

namespace PhiliaContacts.DesktopApp;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        ServiceProvider? serviceProvider = null;

        try
        {
            IServiceCollection services = DependencyInjection.BuildServiceCollection();
            serviceProvider = services.BuildServiceProvider();
            await serviceProvider.InitializePhiliaContactsDataAsync();
            await TryImportLegacyContactsAsync(serviceProvider);
            Ioc.Default.ConfigureServices(serviceProvider);

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            serviceProvider?.Dispose();
            Log.CloseAndFlush();
        }
    }

    //TODO: I'm not sure about even doing this, but even if we stick with it, to me this seems more like an initialization concern and not something
    //the application root should be doing. Maybe this should be moved to the data layer and called from the data layer initialization code.
    private static async Task TryImportLegacyContactsAsync(ServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            ILegacyContactImportService importer = scope.ServiceProvider.GetRequiredService<ILegacyContactImportService>();
            foreach (LegacyImportResult result in await importer.ImportDiscoveredAsync())
            {
                logger.LogInformation("Legacy contact import: {Outcome}, {ContactCount} records.", result.Outcome, result.ContactCount);
            }
        }
        catch (Exception exception)
        {
            // Preserve both databases and source files. A later explicit import can retry after recovery.
            logger.LogError(exception, "Legacy contact import failed. Select the original JSON file for recovery.");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
