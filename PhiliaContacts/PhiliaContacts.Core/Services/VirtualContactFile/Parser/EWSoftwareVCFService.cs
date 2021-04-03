using EWSoftware.PDI.Objects;
using EWSoftware.PDI.Parser;
using EWSoftware.PDI.Properties;
using PhiliaContacts.Core.Base.Extensions;
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
        private readonly ILogger _logger;

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
                    DateTime? birthday = null;
                    if (DateTime.TryParse(vCard.BirthDate?.Value, out DateTime vCardBirthday))
                    {
                        birthday = vCardBirthday;
                    }

                    Contact newContact = new Contact
                    {
                        GivenName = vCard.Name.GivenName,
                        MiddleName = vCard.Name.AdditionalNames,
                        FamilyName = vCard.Name.FamilyName,
                        Nickname = vCard.Nickname.Value,
                        Prefix = vCard.Name.NamePrefix,
                        Suffix = vCard.Name.NameSuffix,
                        EmailAddresses = new ObservableCollection<EmailAddress>(vCard.EMailAddresses.Select(ea => ConvertVCardEmail(ea))),
                        PhoneNumbers = new ObservableCollection<PhoneNumber>(vCard.Telephones.Select(t => ConvertVCardPhone(t))),
                        Birthday = birthday,
                        Title = vCard.Title?.Value,
                        Organization = vCard.Organization?.Name,
                        Photo = ConvertVCardPhoto(vCard.Photo, placeholderImage),
                        TwitterUser = vCard.CustomProperties?.Where(cp => cp.CustomParameters != null && cp.CustomParameters.Contains("TYPE=twitter"))?.FirstOrDefault()?.CustomParameters.GetAfterLastOrEmpty("X-USER=").GetUntilOrEmpty(";"),
                        FacebookUser = vCard.CustomProperties?.Where(cp => cp.CustomParameters != null && cp.CustomParameters.Contains("TYPE=facebook"))?.FirstOrDefault()?.CustomParameters.GetAfterLastOrEmpty("X-USER=").GetUntilOrEmpty(";"),
                        LinkedInUser = vCard.CustomProperties?.Where(cp => cp.CustomParameters != null && cp.CustomParameters.Contains("TYPE=linkedin"))?.FirstOrDefault()?.CustomParameters.GetAfterLastOrEmpty("X-USER=").GetUntilOrEmpty(";"),
                        Url = vCard.Urls?.Where(u => !string.IsNullOrEmpty(u.Value))?.FirstOrDefault()?.Value,
                        Notes = vCard.Notes?.Where(n => !string.IsNullOrEmpty(n.Value))?.FirstOrDefault()?.Value,
                        IsFavorite = vCard.Categories?.Value?.Contains("starred") ?? false
                    };

                    SetAddressFields(newContact, vCard.Addresses);

                    contacts.Add(newContact);
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

        private static void SetAddressFields(Contact contact, AddressPropertyCollection addresses)
        {
            if (addresses != null && addresses.Count > 0)
            {
                AddressProperty addressProperty = addresses.OrderBy(a => a.PreferredOrder).FirstOrDefault();

                contact.AddressType = ConvertVCardAddressType(addressProperty);
                contact.Street = addressProperty.StreetAddress;
                contact.City = addressProperty.Locality;
                contact.State = addressProperty.Region;
                contact.Zip = addressProperty.PostalCode;
                contact.CountryRegion = addressProperty.Country;
            }
        }

        private static Contact.AddressTypes ConvertVCardAddressType(AddressProperty addressProperty)
        {
            Contact.AddressTypes addressType;

            if (addressProperty.AddressTypes.HasFlag(AddressTypes.Home) || addressProperty.AddressTypes.HasFlag(AddressTypes.Preferred))
            {
                addressType = Contact.AddressTypes.Home;
            }
            else if (addressProperty.AddressTypes.HasFlag(AddressTypes.None))
            {
                addressType = Contact.AddressTypes.None;
            }
            else if (addressProperty.AddressTypes.HasFlag(AddressTypes.Domestic))
            {
                addressType = Contact.AddressTypes.Domestic;
            }
            else if (addressProperty.AddressTypes.HasFlag(AddressTypes.International))
            {
                addressType = Contact.AddressTypes.International;
            }
            else if (addressProperty.AddressTypes.HasFlag(AddressTypes.Postal))
            {
                addressType = Contact.AddressTypes.Postal;
            }
            else if (addressProperty.AddressTypes.HasFlag(AddressTypes.Parcel))
            {
                addressType = Contact.AddressTypes.Parcel;
            }
            else if (addressProperty.AddressTypes.HasFlag(AddressTypes.Work))
            {
                addressType = Contact.AddressTypes.Work;
            }
            else
            {
                addressType = Contact.AddressTypes.None;
            }

            return addressType;
        }
    }
}
