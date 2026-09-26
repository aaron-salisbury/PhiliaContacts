using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;

namespace PhiliaContacts.Tests.Architecture;

[TestClass]
public sealed class BusinessArchitectureTests
{
    [TestMethod]
    public void Business_DoesNotReferenceData()
    {
        Assembly businessAssembly = typeof(PhiliaContacts.Business.DependencyInjection).Assembly;

        bool referencesData = businessAssembly
            .GetReferencedAssemblies()
            .Any(assembly => assembly.Name == "PhiliaContacts.Data");

        Assert.IsFalse(referencesData);
    }

    [TestMethod]
    public void Business_DoesNotReferenceIntegrations()
    {
        Assembly businessAssembly = typeof(PhiliaContacts.Business.DependencyInjection).Assembly;

        bool referencesIntegrations = businessAssembly
            .GetReferencedAssemblies()
            .Any(assembly => assembly.Name == "PhiliaContacts.Integrations");

        Assert.IsFalse(referencesIntegrations);
    }

    [TestMethod]
    public void Data_DoesNotReferenceIntegrations()
    {
        Assembly dataAssembly = typeof(PhiliaContacts.Data.DependencyInjection).Assembly;

        bool referencesIntegrations = dataAssembly
            .GetReferencedAssemblies()
            .Any(assembly => assembly.Name == "PhiliaContacts.Integrations");

        Assert.IsFalse(referencesIntegrations);
    }

    [TestMethod]
    public void Integrations_DoesNotReferenceData()
    {
        Assembly integrationsAssembly =
            typeof(PhiliaContacts.Integrations.DependencyInjection).Assembly;

        bool referencesData = integrationsAssembly
            .GetReferencedAssemblies()
            .Any(assembly => assembly.Name == "PhiliaContacts.Data");

        Assert.IsFalse(referencesData);
    }
}
