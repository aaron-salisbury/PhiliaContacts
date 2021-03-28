using EWSoftware.PDI.Objects;
using EWSoftware.PDI.Parser;
using EWSoftware.PDI.Properties;
using PhiliaContacts.Core.Base.Helpers;
using PhiliaContacts.Domains;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhiliaContacts.Core.Services
{
    public class EWSoftwareVCFService : IVCFParserService
    {
        private ILogger _logger { get; set; }

        public EWSoftwareVCFService(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<Contact> GetContactsFromVCFContents(string vcfContents)
        {
            try
            {
                List<Contact> contacts = new List<Contact>();

                _logger.Information("Beginning to convert vCard file.");

                VCardParser vcp = new VCardParser();
                vcp.ParseString(vcfContents);

                byte[] placeholderImage = Images.EmbeddedImageToBytes("PhiliaContacts.Core.Base.Resources.contact-placeholder.png");

                foreach (VCard vCard in vcp.VCards)
                {
                    DateTime.TryParse(vCard.BirthDate?.Value, out DateTime birthDay);

                    contacts.Add(new Contact
                    {
                        GivenName = vCard.Name.GivenName,
                        MiddleName = vCard.Name.AdditionalNames,
                        FamilyName = vCard.Name.FamilyName,
                        Nickname = vCard.Nickname.Value,
                        Prefix = vCard.Name.NamePrefix,
                        Suffix = vCard.Name.NameSuffix,
                        EmailAddresses = new ObservableCollection<EmailAddress>(vCard.EMailAddresses.Select(ea => ConvertVCardEmail(ea))),
                        PhoneNumbers = new ObservableCollection<PhoneNumber>(vCard.Telephones.Select(t => ConvertVCardPhone(t))),
                        Birthday = birthDay,
                        Title = vCard.Title?.Value,
                        Organization = vCard.Organization?.Name,
                        Photo = ConvertVCardPhoto(vCard.Photo, placeholderImage),
                        TwitterUser = vCard.CustomProperties?.Where(cp => cp.CustomParameters != null && cp.CustomParameters.Contains("TYPE=twitter"))?.FirstOrDefault()?.Value,
                        FacebookUser = vCard.CustomProperties?.Where(cp => cp.CustomParameters != null && cp.CustomParameters.Contains("TYPE=facebook"))?.FirstOrDefault()?.Value,
                        LinkedInUser = vCard.CustomProperties?.Where(cp => cp.CustomParameters != null && cp.CustomParameters.Contains("TYPE=linkedin"))?.FirstOrDefault()?.Value,
                        Addresses = new ObservableCollection<Address>(vCard.Addresses.Select(a => ConvertVCardAddress(a))),
                        Url = vCard.Urls?.Where(u => !string.IsNullOrEmpty(u.Value))?.FirstOrDefault()?.Value,
                        Notes = vCard.Notes?.Where(n => !string.IsNullOrEmpty(n.Value))?.FirstOrDefault()?.Value,
                        IsFavorite = vCard.Categories?.Value?.Contains("starred") ?? false
                    });
                }

                _logger.Information("Successfully converted vCard file.");
                return contacts;
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to convert vCard file: {e.Message}");
                return null;
            }
        }

        private static byte[] ConvertVCardPhoto(PhotoProperty photoProperty, byte[] defaultImage)
        {
            if (photoProperty != null && !string.IsNullOrEmpty(photoProperty.Value))
            {
                return Images.ImageUrlToBytes(photoProperty.Value);
            }

            return defaultImage;
        }

        private static EmailAddress ConvertVCardEmail(EMailProperty emailProperty)
        {
            EmailAddress emailAddress = new EmailAddress
            {
                Email = emailProperty.Value
            };

            switch (emailProperty.EMailTypes)
            {
                case EMailTypes.Preferred:
                    emailAddress.Type = EmailAddress.EmailType.Home;
                    break;
                case EMailTypes.None:
                    emailAddress.Type = EmailAddress.EmailType.None;
                    break;
                case EMailTypes.Internet:
                    emailAddress.Type = EmailAddress.EmailType.Internet;
                    break;
                case EMailTypes.AOL:
                    emailAddress.Type = EmailAddress.EmailType.AOL;
                    break;
                case EMailTypes.AppleLink:
                    emailAddress.Type = EmailAddress.EmailType.Applelink;
                    break;
                case EMailTypes.IBMMail:
                    emailAddress.Type = EmailAddress.EmailType.IBMMail;
                    break;
                default:
                    emailAddress.Type = EmailAddress.EmailType.Work;
                    break;
            }

            return emailAddress;
        }

        private static PhoneNumber ConvertVCardPhone(TelephoneProperty phoneProperty)
        {
            PhoneNumber phoneNumber = new PhoneNumber
            {
                Number = phoneProperty.Value
            };

            switch (phoneProperty.PhoneTypes)
            {
                case PhoneTypes.None:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.None;
                    break;
                case PhoneTypes.Work:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Work;
                    break;
                case PhoneTypes.Home:
                case PhoneTypes.Preferred:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Home;
                    break;
                case PhoneTypes.Voice:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Voice;
                    break;
                case PhoneTypes.Fax:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Fax;
                    break;
                case PhoneTypes.Cell:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Cell;
                    break;
                case PhoneTypes.Pager:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Pager;
                    break;
                case PhoneTypes.BBS:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.BBS;
                    break;
                case PhoneTypes.Modem:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Modem;
                    break;
                case PhoneTypes.Car:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Car;
                    break;
                case PhoneTypes.ISDN:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.ISDN;
                    break;
                case PhoneTypes.Video:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Video;
                    break;
                case PhoneTypes.Text:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.Text;
                    break;
                case PhoneTypes.TextPhone:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.TextPhone;
                    break;
                default:
                    phoneNumber.Type = PhoneNumber.PhoneNumberType.None;
                    break;
            }

            return phoneNumber;
        }

        private static Address ConvertVCardAddress(AddressProperty addressProperty)
        {
            Address address = new Address
            {
                Location = addressProperty.Value
            };

            switch (addressProperty.AddressTypes)
            {
                case AddressTypes.None:
                    address.Type = Address.AddressType.None;
                    break;
                case AddressTypes.Domestic:
                    address.Type = Address.AddressType.Domestic;
                    break;
                case AddressTypes.International:
                    address.Type = Address.AddressType.International;
                    break;
                case AddressTypes.Postal:
                    address.Type = Address.AddressType.Postal;
                    break;
                case AddressTypes.Parcel:
                    address.Type = Address.AddressType.Parcel;
                    break;
                case AddressTypes.Home:
                case AddressTypes.Preferred:
                    address.Type = Address.AddressType.Home;
                    break;
                case AddressTypes.Work:
                    address.Type = Address.AddressType.Work;
                    break;
                default:
                    address.Type = Address.AddressType.None;
                    break;
            }

            return address;
        }
    }
}
