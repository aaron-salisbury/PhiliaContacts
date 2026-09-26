using PhiliaContacts.Business;
using PhiliaContacts.Data;
using PhiliaContacts.Integrations;
using PhiliaContacts.Presentation.Desktop;
using PhiliaContacts.Presentation.Desktop.Base.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RunnethOverStudio.AppToolkit.Core;
using RunnethOverStudio.AppToolkit.Modules.Access;
using RunnethOverStudio.AppToolkit.Modules.Messaging;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace PhiliaContacts.DesktopApp;

internal static class DependencyInjection
{
    internal static IServiceCollection BuildServiceCollection()
    {
        string applicationDataDirectory = GetApplicationDataDirectory();

        Serilog.Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(applicationDataDirectory, "log.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        IServiceCollection services = new ServiceCollection();

        // Infrastructure
        services.AddLogging(configure => configure.AddSerilog(Serilog.Log.Logger))
            .AddSingleton((sp) => sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(App)))
            .AddSingleton<IEventSystem, EventSystem>();

        // Data Access
        services.RegisterInternalDataServices(applicationDataDirectory)
            .ComposeDataAccessIntegrations();

        // Business and external capabilities
        services.RegisterInternalBusinessServices()
            .RegisterInternalIntegrationsServices()
            .ComposeBusinessIntegrations();

        // Presentation
        services.RegisterInternalPresentationServices()
            .ComposePresentationIntegrations();

        return services;
    }

    private static IServiceCollection ComposeDataAccessIntegrations(this IServiceCollection services)
    {
        // File System Access
        services.AddScoped<IFileSystemAccess, FileSystemAccess>();

        // Web Access
        services.AddHttpClient(HttpRequester.COMPRESSION_CLIENT_NAME, c => c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip"))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });
        services.AddScoped<IHttpRequester, HttpRequester>();

        return services;
    }

    private static IServiceCollection ComposeBusinessIntegrations(this IServiceCollection services)
    {
        //TODO: Add business-tier integrations here (e.g., external APIs, services, etc.)
        //      Example: services.AddScoped<IBusinessAPIContract, IntegrationsImplementation>();

        return services;
    }

    private static IServiceCollection ComposePresentationIntegrations(this IServiceCollection services)
    {
        services.AddScoped<IAgnosticDispatcher, AvaloniaDispatcher>();

        return services;
    }

    private static string GetApplicationDataDirectory()
    {
        using ILoggerFactory bootstrapLoggerFactory = LoggerFactory.Create(builder =>
            builder.AddProvider(NullLoggerProvider.Instance));

        FileSystemAccess fileSystemAccess = new(bootstrapLoggerFactory.CreateLogger<IFileSystemAccess>());
        ProcessResult<string> appDirectoryPathResult = fileSystemAccess.GetOrCreateAppDirectoryPath();

        if (appDirectoryPathResult.IsSuccessful)
        {
            return appDirectoryPathResult.Value;
        }

        throw new InvalidOperationException("Failed to get or create application data directory.", appDirectoryPathResult.Error);
    }
}
