using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Integrations;
using PhiliaContacts.Presentation.Desktop.ViewModels;
using System.IO;
using System.Linq;
using System.Text;

namespace PhiliaContacts.Tests.Contacts;

[TestClass]
public sealed class ContactUiAndVCardTests
{
    [TestMethod]
    public void ContactDetailsOwnTheirCollectionsAndPhotoBytes()
    {
        Contact original = new()
        {
            Id = ContactId.New(),
            GivenName = "Original",
            PhoneticGivenName = "Example",
            PhoneNumbers = [new ContactValue("555-0100", "Home")],
            EmailAddresses = [new ContactValue("old@example.invalid", "Work")],
            Addresses = [new ContactAddress("Home", "Street", "City", "WI", "00000", "US")]
        };

        using ContactDetailViewModel first = new(original);
        using ContactDetailViewModel second = new(original);

        first.PhoneNumbers[0].Value = "555-0199";
        first.EmailAddresses.Clear();
        first.Addresses[0].Street = "Changed";
        first.GivenName = "Changed";

        Assert.AreEqual("555-0100", second.PhoneNumbers[0].Value);
        Assert.AreEqual("555-0100", original.PhoneNumbers[0].Value);
        Assert.HasCount(1, second.EmailAddresses);
        Assert.AreEqual("Street", second.Addresses[0].Street);
        Assert.AreEqual("Original", original.GivenName);
        Assert.AreEqual("555-0199", first.ToContact().PhoneNumbers.Single().Value);
        using ContactDetailViewModel newContact = new();
        Assert.IsEmpty(newContact.PhoneNumbers);
        Assert.AreNotEqual(original.Id, newContact.ToContact().Id);
    }

    [TestMethod]
    public void VCard_HandlesUtf8FoldingEscapingPhotosPhoneticNamesAndExtensions()
    {
        using ServiceProvider services = new ServiceCollection().RegisterInternalIntegrationsServices().BuildServiceProvider();
        IVCardContactService vCard = services.GetRequiredService<IVCardContactService>();
        string notes = string.Concat(Enumerable.Repeat("汉字,;\\\n", 18));

        Contact source = new()
        {
            Id = ContactId.New(),
            GivenName = "José",
            FamilyName = "Example",
            PhoneticGivenName = "Ho-se",
            PhoneticFamilyName = "Eg-zam-pul",
            Birthday = "2000-01-01",
            Notes = notes,
            PhoneNumbers = [new ContactValue("+1 555-0100", "Cell"), new ContactValue("555-0101", "Work")],
            EmailAddresses = [new ContactValue("jose@example.invalid", "Internet")],
            Addresses = [new ContactAddress("Home", "A; B, C", "City", "WI", "00000", "US")],
            Photo = [0xff, 0xd8, 0xff, 0xd9],
            VCardProperties = ["X-TEST:retained"]
        };

        string text = vCard.Write([source]);
        Assert.Contains("\r\n ", text);
        Assert.Contains("PHOTO;ENCODING=b;TYPE=JPEG:", text);

        foreach (string line in text.Split("\r\n"))
        {
            Assert.IsLessThanOrEqualTo(75, Encoding.UTF8.GetByteCount(line));
        }

        Contact loaded = vCard.Read(text).Single();
        Assert.AreEqual("José", loaded.GivenName);
        Assert.AreEqual(source.PhoneticFamilyName, loaded.PhoneticFamilyName);
        Assert.AreEqual(notes, loaded.Notes);
        Assert.HasCount(2, loaded.PhoneNumbers);
        Assert.AreEqual("A; B, C", loaded.Addresses.Single().Street);
        CollectionAssert.AreEqual(source.Photo!, loaded.Photo!);
        CollectionAssert.AreEqual(source.VCardProperties.ToArray(), loaded.VCardProperties.ToArray());
    }

    [TestMethod]
    public void VCard_RejectsIncompleteCardsAndPreservesDistinctContacts()
    {
        using ServiceProvider services = new ServiceCollection().RegisterInternalIntegrationsServices().BuildServiceProvider();
        IVCardContactService vCard = services.GetRequiredService<IVCardContactService>();
        Assert.ThrowsExactly<InvalidDataException>(() => vCard.Read("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Missing end\r\n"));
        Contact[] contacts = [.. vCard.Read("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Same\r\nEND:VCARD\r\nBEGIN:VCARD\r\nVERSION:4.0\r\nFN:Same\r\nEND:VCARD\r\n")];
        Assert.HasCount(2, contacts);
        Assert.AreNotEqual(contacts[0].Id, contacts[1].Id);
    }
}
