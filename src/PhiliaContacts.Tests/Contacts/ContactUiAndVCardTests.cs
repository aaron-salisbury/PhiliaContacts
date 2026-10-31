using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Integrations;
using PhiliaContacts.Presentation.Desktop.Base.Converters;
using PhiliaContacts.Presentation.Desktop.ViewModels;
using System;
using System.Globalization;
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
        Assert.AreEqual(source.Id, loaded.Id);
        CollectionAssert.AreEqual(new[] { $"UID:urn:uuid:{source.Id.Value:D}", "X-TEST:retained" }, loaded.VCardProperties.ToArray());

        // A second export reuses the retained UID instead of adding another.
        Assert.AreEqual(1, vCard.Write([loaded]).Split("\r\n").Count(line => line.StartsWith("UID:", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void VCard_ReimportUsesUidAsStableIdentity()
    {
        IVCardContactService vCard = CreateVCardService();
        const string card = "BEGIN:VCARD\r\nVERSION:3.0\r\nUID:google-1234abcd\r\nFN:Stable\r\nEND:VCARD\r\n";

        Assert.AreEqual(vCard.Read(card).Single().Id, vCard.Read(card).Single().Id);
    }

    [TestMethod]
    public void ImportMerge_MatchesStoredContactsByRetainedUidAndMergesDuplicateCards()
    {
        IVCardContactService vCard = CreateVCardService();

        // Saved under the earlier import behavior: random ID, UID kept only as a retained property.
        Contact earlierImport = new() { Id = ContactId.New(), GivenName = "Earlier", Notes = "Kept", VCardProperties = ["UID:apple-42"] };
        Contact[] parsed = [.. vCard.Read(
            "BEGIN:VCARD\r\nVERSION:3.0\r\nUID:apple-42\r\nFN:Earlier\r\nTITLE:Updated\r\nEND:VCARD\r\n" +
            "BEGIN:VCARD\r\nVERSION:3.0\r\nUID:dup\r\nFN:Dup\r\nTEL:555-0001\r\nEND:VCARD\r\n" +
            "BEGIN:VCARD\r\nVERSION:3.0\r\nUID:dup\r\nFN:Dup\r\nEMAIL:dup@example.invalid\r\nEND:VCARD\r\n")];

        Contact[] result = [.. ContactImportMerge.MergeIntoStored([earlierImport], parsed)];

        Assert.HasCount(2, result);
        Contact updated = result.Single(contact => contact.Id == earlierImport.Id);
        Assert.AreEqual("Updated", updated.Title);
        Assert.AreEqual("Kept", updated.Notes);
        Contact duplicate = result.Single(contact => contact.Id != earlierImport.Id);
        Assert.HasCount(1, duplicate.PhoneNumbers);
        Assert.HasCount(1, duplicate.EmailAddresses);
    }

    [TestMethod]
    public void ImportMerge_KeepsStoredValuesTheCardOmitsOrSkipped()
    {
        IVCardContactService vCard = CreateVCardService();
        Contact incoming = vCard.Read("BEGIN:VCARD\r\nVERSION:3.0\r\nUID:merge-1\r\nN:Example;New;;;\r\n" +
            "PHOTO:https://example.invalid/photo.jpg\r\nTITLE:Updated\r\nTEL;TYPE=CELL:(555) 0100\r\nTEL;TYPE=WORK:555-0199\r\nEND:VCARD\r\n").Single();
        Contact stored = new()
        {
            Id = incoming.Id,
            GivenName = "Old",
            Notes = "Local notes",
            Title = "Previous",
            Photo = [0xff, 0xd8, 0xff, 0xd9],
            IsFavorite = true,
            PhoneNumbers = [new ContactValue("555-0100", "Home"), new ContactValue("555-0142", "Local only")],
            VCardProperties = ["X-LOCAL:kept", "UID:merge-1", "item1.X-ABLabel:Old one", "item2.X-ABLabel:Two"]
        };
        incoming = incoming with { VCardProperties = [.. incoming.VCardProperties, "item1.X-ABLabel:New one"] };

        Contact merged = ContactImportMerge.Merge(stored, incoming);

        Assert.AreEqual("New", merged.GivenName);
        Assert.AreEqual("Updated", merged.Title);
        Assert.AreEqual("Local notes", merged.Notes);
        CollectionAssert.AreEqual(stored.Photo, merged.Photo!);
        Assert.IsTrue(merged.IsFavorite);
        CollectionAssert.AreEqual(new[] { "555-0100", "555-0142", "555-0199" }, merged.PhoneNumbers.Select(phone => phone.Value).ToArray());
        CollectionAssert.AreEqual(new[] { "X-LOCAL:kept", "item2.X-ABLabel:Two", "UID:merge-1", "item1.X-ABLabel:New one" }, merged.VCardProperties.ToArray());
    }

    [TestMethod]
    public void VCard_SkipsRemotePhotosAndKeepsUnpreviewableOnes()
    {
        IVCardContactService vCard = CreateVCardService();
        string gif = Convert.ToBase64String("GIF89a"u8.ToArray());
        string text = "BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Remote\r\nPHOTO:https://lh3.googleusercontent.com/photo.jpg\r\nEND:VCARD\r\n" +
            "BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Gif\r\nPHOTO;ENCODING=b;TYPE=GIF:" + gif + "\r\nEND:VCARD\r\n" +
            "BEGIN:VCARD\r\nVERSION:4.0\r\nFN:Uri\r\nPHOTO;VALUE=uri:http://example.invalid/a.png\r\nEND:VCARD\r\n";

        Contact[] contacts = [.. vCard.Read(text)];

        Assert.HasCount(3, contacts);
        Assert.IsNull(contacts[0].Photo);
        CollectionAssert.AreEqual("GIF89a"u8.ToArray(), contacts[1].Photo!);
        Assert.IsNull(contacts[2].Photo);
    }

    [TestMethod]
    public void VCard_FavoriteCategoryCoexistsWithOtherCategories()
    {
        IVCardContactService vCard = CreateVCardService();
        Contact loaded = vCard.Read("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Fav\r\nCATEGORIES:Family,starred\r\nEND:VCARD\r\n").Single();

        Assert.IsTrue(loaded.IsFavorite);
        Assert.Contains("CATEGORIES:Family", loaded.VCardProperties);

        string[] categoryLines = [.. vCard.Write([loaded]).Split("\r\n").Where(line => line.StartsWith("CATEGORIES", StringComparison.Ordinal))];
        CollectionAssert.AreEqual(new[] { "CATEGORIES:starred,Family" }, categoryLines);
    }

    [TestMethod]
    public void VCard_ChoosesLabelTypesOverCapabilityTypes()
    {
        IVCardContactService vCard = CreateVCardService();
        Contact loaded = vCard.Read("BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Types\r\n" +
            "EMAIL;type=INTERNET;type=HOME;type=pref:home@example.invalid\r\n" +
            "TEL;TYPE=\"voice,cell\":555-0100\r\n" +
            "TEL;TYPE=pref:555-0101\r\nEND:VCARD\r\n").Single();

        Assert.AreEqual("Home", loaded.EmailAddresses.Single().Type);
        Assert.AreEqual("Cell", loaded.PhoneNumbers[0].Type);
        Assert.AreEqual("Pref", loaded.PhoneNumbers[1].Type);
    }

    [TestMethod]
    public void VCard_RoundTripsSocialProfilesBirthdayAndUnescapedUrl()
    {
        IVCardContactService vCard = CreateVCardService();
        Contact source = new()
        {
            Id = ContactId.New(),
            GivenName = "Social",
            TwitterUser = "example_x",
            FacebookUser = "example.fb",
            LinkedInUser = "example-li",
            Birthday = "2000-01-01T13:03:02.7850172",
            Url = "https://example.invalid/a,b"
        };

        string text = vCard.Write([source]);
        Assert.Contains("BDAY:2000-01-01\r\n", text);
        Assert.Contains("URL:https://example.invalid/a,b\r\n", text);

        Contact loaded = vCard.Read(text).Single();
        Assert.AreEqual("example_x", loaded.TwitterUser);
        Assert.AreEqual("example.fb", loaded.FacebookUser);
        Assert.AreEqual("example-li", loaded.LinkedInUser);
        Assert.AreEqual("2000-01-01", loaded.Birthday);
        Assert.AreEqual(source.Url, loaded.Url);
        Assert.IsFalse(loaded.VCardProperties.Any(line => line.StartsWith("X-SOCIALPROFILE", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void BirthdayConverter_UsesCalendarDateRegardlessOfCultureOrOffset()
    {
        BirthdayToDateConverter converter = new();
        CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");

        DateTimeOffset? shown = (DateTimeOffset?)converter.Convert("2000-01-01T13:03:02.7850172", typeof(DateTimeOffset?), null, culture);
        Assert.AreEqual(new DateTime(2000, 1, 1), shown!.Value.DateTime);
        Assert.IsNull(converter.Convert("--0115", typeof(DateTimeOffset?), null, culture));

        DateTimeOffset picked = new(1990, 1, 15, 23, 30, 0, TimeSpan.FromHours(-6));
        Assert.AreEqual("1990-01-15", converter.ConvertBack(picked, typeof(string), null, culture));
    }

    [TestMethod]
    public void ContactDetails_OpenWithUnsupportedPhotoAndTrackUnsavedChanges()
    {
        byte[] gif = [.. "GIF89a"u8];
        using ContactDetailViewModel detail = new(new Contact { Id = ContactId.New(), GivenName = "Gif", Photo = gif, PhoneNumbers = [new ContactValue("555", "Home")] });

        Assert.IsNull(detail.PhotoPreview);
        CollectionAssert.AreEqual(gif, detail.ToContact().Photo!);
        Assert.IsFalse(detail.IsDirty);

        detail.PhoneNumbers[0].Value = "556";
        Assert.IsTrue(detail.IsDirty);

        detail.AcceptChanges();
        detail.SelectedPhone = detail.PhoneNumbers[0];
        Assert.IsFalse(detail.IsDirty);

        detail.AddPhoneCommand.Execute(null);
        Assert.IsTrue(detail.IsDirty);
    }

    private static IVCardContactService CreateVCardService()
    {
        return new ServiceCollection().RegisterInternalIntegrationsServices().BuildServiceProvider().GetRequiredService<IVCardContactService>();
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
