using Avalonia;
using CommunityToolkit.Mvvm.DependencyInjection;
using PhiliaContacts.Data.Database;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Presentation.Desktop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
