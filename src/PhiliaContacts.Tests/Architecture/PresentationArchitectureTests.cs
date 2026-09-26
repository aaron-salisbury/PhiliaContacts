using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;

namespace PhiliaContacts.Tests.Architecture;

[TestClass]
public sealed class PresentationArchitectureTests
{
    [TestMethod]
    public void DesktopPresentation_DoesNotReferenceData()
    {
        Assembly presentationAssembly = typeof(PhiliaContacts.Presentation.Desktop.App).Assembly;

        bool referencesData = presentationAssembly
            .GetReferencedAssemblies()
            .Any(assembly => assembly.Name == "PhiliaContacts.Data");

        Assert.IsFalse(referencesData);
    }

    [TestMethod]
    public void DesktopPresentation_DoesNotReferenceIntegrations()
    {
        Assembly presentationAssembly = typeof(PhiliaContacts.Presentation.Desktop.App).Assembly;

        bool referencesIntegrations = presentationAssembly
            .GetReferencedAssemblies()
            .Any(assembly => assembly.Name == "PhiliaContacts.Integrations");

        Assert.IsFalse(referencesIntegrations);
    }
}
