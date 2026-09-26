using PhiliaContacts.Business;
using PhiliaContacts.Business.Modules.Integrations;
using PhiliaContacts.Data;
using PhiliaContacts.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace PhiliaContacts.Tests.Architecture;

[TestClass]
public sealed class IntegrationCompositionTests
{
    [TestMethod]
    public void RegisteredIntegrationServices_ResolveThroughComposition()
    {
        string applicationDataDirectory = Path.Combine(Path.GetTempPath(), $"PhiliaContacts-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(applicationDataDirectory);

        try
        {
            ServiceCollection services = new();
            services.AddHttpClient();
            services.RegisterInternalDataServices(applicationDataDirectory)
                .RegisterInternalBusinessServices()
                .RegisterInternalIntegrationsServices();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Assert.IsNotNull(serviceProvider.GetRequiredService<IIntegrationService>());

            IProviderRegistry registry = serviceProvider.GetRequiredService<IProviderRegistry>();
            Assert.IsNotEmpty(registry.Providers);
        }
        finally
        {
            Directory.Delete(applicationDataDirectory, true);
        }
    }
}
