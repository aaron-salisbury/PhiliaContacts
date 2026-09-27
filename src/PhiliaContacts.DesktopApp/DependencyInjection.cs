using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PhiliaContacts.Business;
using PhiliaContacts.Data;
using PhiliaContacts.Integrations;
using PhiliaContacts.Presentation.Desktop;
using RunnethOverStudio.AppToolkit.Core;
using RunnethOverStudio.AppToolkit.Modules.Access;
using Serilog;
using System;
using System.IO;

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
        services.AddLogging(configure => configure.AddSerilog(Serilog.Log.Logger));
        services.RegisterInternalDataServices(applicationDataDirectory)
            .RegisterInternalBusinessServices()
            .RegisterInternalIntegrationsServices()
            .RegisterInternalPresentationServices();
        return services;
    }

    private static string GetApplicationDataDirectory()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(NullLoggerProvider.Instance));
        FileSystemAccess fileSystemAccess = new(loggerFactory.CreateLogger<IFileSystemAccess>());
        ProcessResult<string> result = fileSystemAccess.GetOrCreateAppDirectoryPath();
        return result.IsSuccessful ? result.Value : throw new InvalidOperationException("Failed to create application data directory.", result.Error);
    }
}
