using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhiliaContacts.Business;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Data;
using PhiliaContacts.Data.Database;
using PhiliaContacts.Integrations;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhiliaContacts.Tests.Contacts;

[TestClass]
public sealed class ContactFoundationTests
{
    [TestMethod]
    public void LegacyReader_PreservesRealFirstReleaseShapeWithoutNewtonsoft()
    {
        using ServiceProvider provider = new ServiceCollection().RegisterInternalIntegrationsServices().BuildServiceProvider();
        ILegacyContactReader reader = provider.GetRequiredService<ILegacyContactReader>();
        string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "legacy", "first-release", "Contact.json"));
        Contact contact = reader.Read(json).Single();

        Assert.AreEqual("Historical", contact.GivenName);
        Assert.AreEqual("2000-01-01T13:03:02.7850172", contact.Birthday);
        Assert.HasCount(2, contact.EmailAddresses);
        Assert.AreEqual("Internet", contact.EmailAddresses[0].Type);
        Assert.AreEqual("None", contact.EmailAddresses[1].Type);
        Assert.AreEqual("Home", contact.PhoneNumbers.Single().Type);
        Assert.AreEqual("Home", contact.Addresses.Single().Type);
        CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71 }, contact.Photo![..4]);
        Assert.AreNotEqual(Guid.Empty, contact.Id.Value);
    }

    [TestMethod]
    public void LegacyReader_RejectsInvalidJsonAndDoesNotMergeSimilarContacts()
    {
        using ServiceProvider provider = new ServiceCollection().RegisterInternalIntegrationsServices().BuildServiceProvider();
        ILegacyContactReader reader = provider.GetRequiredService<ILegacyContactReader>();
        Assert.ThrowsExactly<JsonException>(() => reader.Read("{}"));
        Contact[] contacts = reader.Read("[{\"GivenName\":\"Same\"},{\"GivenName\":\"Same\"}]").ToArray();
        Assert.HasCount(2, contacts);
        Assert.AreNotEqual(contacts[0].Id, contacts[1].Id);
    }

    [TestMethod]
    public async Task ContactStore_SavesChildrenAndPhotosAtomicallyAndDeletesOnlySelectedContact()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"PhiliaContacts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            ServiceCollection services = new();
            services.RegisterInternalDataServices(directory).RegisterInternalBusinessServices();
            using ServiceProvider provider = services.BuildServiceProvider();
            await provider.InitializePhiliaContactsDataAsync();
            IContactService service = provider.GetRequiredService<IContactService>();
            Contact first = new()
            {
                Id = ContactId.New(), GivenName = "First", FamilyName = "Test", IsFavorite = true,
                Birthday = "2000-01-01T13:03:02.7850172", Photo = [1, 2, 3],
                PhoneNumbers = [new ContactValue("+1 555-0100", "Cell"), new ContactValue("555-0101", "Home")],
                EmailAddresses = [new ContactValue("first@example.invalid", "Internet")],
                Addresses = [new ContactAddress("Home", "Street", "City", "WI", "00000", "US")]
            };
            Contact second = new() { Id = ContactId.New(), GivenName = "Second" };
            await service.SaveAsync(first);
            await service.SaveAsync(second);
            Contact loaded = (await service.GetAsync(first.Id))!;
            Assert.HasCount(2, loaded.PhoneNumbers);
            Assert.AreEqual("555-0101", loaded.PhoneNumbers[1].Value);
            Assert.AreEqual("Street", loaded.Addresses.Single().Street);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, loaded.Photo!);
            Assert.AreEqual(first.Birthday, loaded.Birthday);

            await service.SaveAsync(first with { PhoneNumbers = [new ContactValue("555-0111", "Work")], Photo = null });
            loaded = (await service.GetAsync(first.Id))!;
            Assert.HasCount(1, loaded.PhoneNumbers);
            Assert.IsNull(loaded.Photo);
            Assert.HasCount(2, await service.ListAsync());
            Assert.IsTrue(await service.DeleteAsync(first.Id));
            Assert.IsNull(await service.GetAsync(first.Id));
            Assert.IsNotNull(await service.GetAsync(second.Id));
            Assert.IsFalse(await service.DeleteAsync(first.Id));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task ContactService_RejectsUnnamedContactWithoutWriting()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"PhiliaContacts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            ServiceCollection services = new();
            services.RegisterInternalDataServices(directory).RegisterInternalBusinessServices();
            using ServiceProvider provider = services.BuildServiceProvider();
            await provider.InitializePhiliaContactsDataAsync();
            IContactService service = provider.GetRequiredService<IContactService>();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => service.SaveAsync(new Contact { Id = ContactId.New() }));
            Assert.IsEmpty(await service.ListAsync());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
