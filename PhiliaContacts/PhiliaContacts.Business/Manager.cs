using PhiliaContacts.Business.Base;
using PhiliaContacts.Business.DTOs;
using PhiliaContacts.Business.Models;
using PhiliaContacts.Data;
using System.Collections.ObjectModel;

namespace PhiliaContacts.Business
{
    public static class Manager
    {
        public static string? AppVersion { get; set; }

        public static IHttpClientFactory? HttpClientFactory { get; set; }

        public static IEnumerable<string> CountryNames { get; set; } = Access.ReadCountryNames();

        public static byte[]? DefaultUserImage { get; set; } = Access.ReadContactImage();

        public static async Task<IEnumerable<Contact>> GetContactsAsync(bool isDebug = false, bool saveDebugData = false)
        {
            if (isDebug)
            {
                List<Contact> contacts = CreateTestData();

                if (saveDebugData)
                {
                    await SaveAsync(contacts);
                }

                return contacts;
            }

            return await LoadContactsAsync();
        }

        public static string GetCurrentUserStorageDirectory()
        {
            return CRUD.GetCurrentUserStorageDirectory();
        }

        public static async Task UpdateUserStorageDirectoryAsync(string newUserStorageDirectory)
        {
            await CRUD.UpdateUserStorageDirectoryAsync<AppData>(newUserStorageDirectory);
        }

        public static async Task<bool> SaveAsync(IEnumerable<Contact> contacts)
        {
            return await CRUD.UpdateDataAsync(new AppData() { Version = AppVersion, Contacts = contacts });
        }

        private static async Task<IEnumerable<Contact>> LoadContactsAsync()
        {
            AppData? loadedAppData = await CRUD.ReadDataAsync<AppData>();

            if (loadedAppData != null && loadedAppData.Contacts.Any())
            {
                return loadedAppData.Contacts;
            }
            else
            {
                // Save file may be in the legacy format of just a collection.
                List<Contact>? legacyContacts = await CRUD.ReadLegacyDataAsync<AppData, List<Contact>>();

                if (legacyContacts != null && legacyContacts.Any() && await SaveAsync(legacyContacts))
                {
                    return legacyContacts;
                }
            }

            return new List<Contact>();
        }

        private static List<Contact> CreateTestData()
        {
            return new List<Contact>
            {
                new Contact
                {
                    FamilyName = "Cashmere",
                    GivenName = "Lucius",
                    Title = "Shredded Cheese Authority",
                    Organization = "Amalgamate Labs",
                    Photo = Access.ReadContactImage("man-1.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "lucius.cashmere@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Url = "http://www.amalgamatelabs.com",
                    Birthday = new DateTime(1985, 2, 28),
                    TwitterUser = "msdev",
                    FacebookUser = "MSFTDev.US"
                },
                new Contact
                {
                    FamilyName = "MacApple",
                    GivenName = "Dell",
                    Title = "Cat Behavior Consultant",
                    Organization = "Amalgamate Labs",
                    Photo = Access.ReadContactImage("man-2.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "dell.macapple@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Url = "http://www.amalgamatelabs.com",
                    Birthday = new DateTime(1985, 2, 28),
                    TwitterUser = "msdev",
                    FacebookUser = "MSFTDev.US"
                },
                new Contact
                {
                    FamilyName = "Mascara",
                    GivenName = "Natasha",
                    Title = "Bread Scientist",
                    Organization = "Amalgamate Labs",
                    Photo = Access.ReadContactImage("woman-1.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "natasha.mascara@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Url = "http://www.amalgamatelabs.com",
                    Birthday = new DateTime(1985, 2, 28),
                    TwitterUser = "msdev",
                    FacebookUser = "MSFTDev.US"
                },
                new Contact
                {
                    FamilyName = "Skinner",
                    GivenName = "Kat",
                    Nickname = "Mom",
                    IsFavorite = true,
                    Photo = Access.ReadContactImage("woman-2.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "kat.skinner@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Birthday = new DateTime(1960, 2, 28),
                    TwitterUser = "msdev",
                    FacebookUser = "MSFTDev.US"
                },
            };
        }
    }
}
