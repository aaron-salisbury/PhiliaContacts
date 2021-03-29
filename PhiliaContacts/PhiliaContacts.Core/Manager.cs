using PhiliaContacts.Core.Base;
using PhiliaContacts.Core.Base.Helpers;
using PhiliaContacts.Domains;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PhiliaContacts.Core
{
    public class Manager : ObservableObject
    {
        internal ILogger Logger { get; set; }

        public byte[] DefaultContactImage { get; set; }

        private ObservableCollection<Contact> _contacts;
        public ObservableCollection<Contact> Contacts
        {
            get { return _contacts; }
            set
            {
                _contacts = value;
                RaisePropertyChanged();
            }
        }

        public Manager(AppLogger appLogger)
        {
            Logger = appLogger.Logger;

            DefaultContactImage = Images.EmbeddedImageToBytes("PhiliaContacts.Core.Base.Resources.contact-placeholder.png");

            //Load();
            LoadTestData();
        }

        public bool Save()
        {
            try
            {
                Logger.Information("Saving contacts.");

                Task.Run(() => Data.CRUD.UpdateDomainsAsync<Contact>(Contacts)).Wait();

                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to save contacts: {e.Message}");
                return false;
            }
        }

        private bool Load()
        {
            try
            {
                Logger.Information("Loading contacts.");

                Contacts = new ObservableCollection<Contact>(Task.Run(() => Data.CRUD.ReadDomainsAsync<Contact>()).Result);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load contacts: {e.Message}");
                return false;
            }
        }

        private void LoadTestData()
        {
            Contacts = new ObservableCollection<Contact>
            {
                new Contact
                {
                    FamilyName = "Cashmere",
                    GivenName = "Lucius",
                    Title = "Shredded Cheese Authority",
                    Organization = "Amalgamate Labs",
                    Photo = Images.EmbeddedImageToBytes("PhiliaContacts.Core.Base.Resources.man-1.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "lucius.cashmere@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Url = "http://www.amalgamatelabs.com",
                    Birthday = new DateTime(1985, 2, 28),
                    TwitterUser = "https://twitter.com/msdev",
                    FacebookUser = "https://www.facebook.com/MSFTDev.US"
                },
                new Contact
                {
                    FamilyName = "MacApple",
                    GivenName = "Dell",
                    Title = "Cat Behavior Consultant",
                    Organization = "Amalgamate Labs",
                    Photo = Images.EmbeddedImageToBytes("PhiliaContacts.Core.Base.Resources.man-2.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "dell.macapple@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Url = "http://www.amalgamatelabs.com",
                    Birthday = new DateTime(1985, 2, 28),
                    TwitterUser = "https://twitter.com/msdev",
                    FacebookUser = "https://www.facebook.com/MSFTDev.US"
                },
                new Contact
                {
                    FamilyName = "Mascara",
                    GivenName = "Natasha",
                    Title = "Bread Scientist",
                    Organization = "Amalgamate Labs",
                    Photo = Images.EmbeddedImageToBytes("PhiliaContacts.Core.Base.Resources.woman-1.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "natasha.mascara@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Url = "http://www.amalgamatelabs.com",
                    Birthday = new DateTime(1985, 2, 28),
                    TwitterUser = "https://twitter.com/msdev",
                    FacebookUser = "https://www.facebook.com/MSFTDev.US"
                },
                new Contact
                {
                    FamilyName = "Skinner",
                    GivenName = "Kat",
                    Nickname = "Mom",
                    IsFavorite = true,
                    Photo = Images.EmbeddedImageToBytes("PhiliaContacts.Core.Base.Resources.woman-2.jpeg"),
                    EmailAddresses = new ObservableCollection<EmailAddress> { new EmailAddress { Email = "kat.skinner@fakeemail.com", Type = EmailAddress.EmailType.Home } },
                    PhoneNumbers = new ObservableCollection<PhoneNumber> { new PhoneNumber { Number = "(555) 555-5555", Type = PhoneNumber.PhoneNumberType.Home } },
                    Birthday = new DateTime(1960, 2, 28),
                    TwitterUser = "https://twitter.com/msdev",
                    FacebookUser = "https://www.facebook.com/MSFTDev.US"
                },
            };
        }
    }
}
