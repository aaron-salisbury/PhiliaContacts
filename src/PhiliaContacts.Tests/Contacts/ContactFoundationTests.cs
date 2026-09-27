using Microsoft.Data.Sqlite;
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
        Contact[] contacts = [.. reader.Read("[{\"GivenName\":\"Same\"},{\"GivenName\":\"Same\"}]")];
        Assert.HasCount(2, contacts);
        Assert.AreNotEqual(contacts[0].Id, contacts[1].Id);
    }

    [TestMethod]
    public void LegacyReader_ParsesObservedLaterReleaseShapeWithFavoriteFirst()
    {
        using ServiceProvider provider = new ServiceCollection().RegisterInternalIntegrationsServices().BuildServiceProvider();
        ILegacyContactReader reader = provider.GetRequiredService<ILegacyContactReader>();
        string earlierJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "legacy", "first-release", "Contact.json"));
        string laterJson = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "legacy", "current", "observed-1.0.8.0", "PhiliaContacts.json"));
        using JsonDocument document = JsonDocument.Parse(laterJson);
        Assert.AreEqual("IsFavorite", document.RootElement[0].EnumerateObject().First().Name);

        Contact earlier = reader.Read(earlierJson).Single();
        Contact later = reader.Read(laterJson).Single();
        Assert.AreEqual(earlier.GivenName, later.GivenName);
        Assert.AreEqual(earlier.FamilyName, later.FamilyName);
        Assert.AreEqual(earlier.Birthday, later.Birthday);
        Assert.AreEqual(earlier.IsFavorite, later.IsFavorite);
        CollectionAssert.AreEqual(earlier.EmailAddresses.ToArray(), later.EmailAddresses.ToArray());
        CollectionAssert.AreEqual(earlier.PhoneNumbers.ToArray(), later.PhoneNumbers.ToArray());
        CollectionAssert.AreEqual(earlier.Addresses.ToArray(), later.Addresses.ToArray());
        CollectionAssert.AreEqual(earlier.Photo!, later.Photo!);
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
            await provider.InitializePhiliaContactsDataAsync(TestContext.CancellationToken);
            IContactService service = provider.GetRequiredService<IContactService>();
            Contact first = new()
            {
                Id = ContactId.New(),
                GivenName = "First",
                FamilyName = "Test",
                IsFavorite = true,
                Birthday = "2000-01-01T13:03:02.7850172",
                Photo = [1, 2, 3],
                PhoneNumbers = [new ContactValue("+1 555-0100", "Cell"), new ContactValue("555-0101", "Home")],
                EmailAddresses = [new ContactValue("first@example.invalid", "Internet")],
                Addresses = [new ContactAddress("Home", "Street", "City", "WI", "00000", "US")]
            };
            Contact second = new() { Id = ContactId.New(), GivenName = "Second" };
            await service.SaveAsync(first, TestContext.CancellationToken);
            await service.SaveAsync(second, TestContext.CancellationToken);
            Contact loaded = (await service.GetAsync(first.Id, TestContext.CancellationToken))!;
            Assert.HasCount(2, loaded.PhoneNumbers);
            Assert.AreEqual("555-0101", loaded.PhoneNumbers[1].Value);
            Assert.AreEqual("Street", loaded.Addresses.Single().Street);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, loaded.Photo!);
            Assert.AreEqual(first.Birthday, loaded.Birthday);

            await service.SaveAsync(first with { PhoneNumbers = [new ContactValue("555-0111", "Work")], Photo = null }, TestContext.CancellationToken);
            loaded = (await service.GetAsync(first.Id, TestContext.CancellationToken))!;
            Assert.HasCount(1, loaded.PhoneNumbers);
            Assert.IsNull(loaded.Photo);
            Assert.HasCount(2, await service.ListAsync(TestContext.CancellationToken));
            Assert.IsTrue(await service.DeleteAsync(first.Id, TestContext.CancellationToken));
            Assert.IsNull(await service.GetAsync(first.Id, TestContext.CancellationToken));
            Assert.IsNotNull(await service.GetAsync(second.Id, TestContext.CancellationToken));
            Assert.IsFalse(await service.DeleteAsync(first.Id, TestContext.CancellationToken));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
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
            await provider.InitializePhiliaContactsDataAsync(TestContext.CancellationToken);
            IContactService service = provider.GetRequiredService<IContactService>();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => service.SaveAsync(new Contact { Id = ContactId.New() }, TestContext.CancellationToken));
            Assert.IsEmpty(await service.ListAsync(TestContext.CancellationToken));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, true);
        }
    }

    public TestContext TestContext { get; set; }
}
