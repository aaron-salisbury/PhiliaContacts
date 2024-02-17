using EWSoftware.PDI.Objects;
using EWSoftware.PDI.Parser;
using EWSoftware.PDI.Properties;
using PhiliaContacts.Business.Base.Extensions;
using PhiliaContacts.Business.Models;
using PhiliaContacts.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace PhiliaContacts.Business.Services.VirtualContactFile
{
    public class EWSoftwareVCFService : IVCFParserService
    {
        public IEnumerable<Contact> GetContactsFromVCFContents(string vcfContents)
        {
            List<Contact> contacts = new();

            try
            {
                Debug.WriteLine("Beginning to convert vCard file.", "INFO");

                VCardParser vcp = new();
                vcp.ParseString(vcfContents);

                byte[]? placeholderImage = Access.ReadContactImage();

                foreach (VCard vCard in vcp.VCards)
                {
                    DateTime? birthday = null;
                    if (DateTime.TryParse(vCard.BirthDate?.Value, out DateTime vCardBirthday))
                    {
                        birthday = vCardBirthday;
                    }

                    Contact newContact = new()
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

                Debug.WriteLine("Successfully converted vCard file.", "INFO");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to convert vCard file: {e.Message}", "ERROR");
                contacts = new List<Contact>();
            }

            return contacts;
        }

        private static byte[]? ConvertVCardPhoto(PhotoProperty photoProperty, byte[]? defaultImage)
        {
            if (photoProperty != null && !string.IsNullOrEmpty(photoProperty.Value))
            {
                try
                {
                    if (photoProperty.Value.ToUpper().Contains("ENCODING=BASE64"))
                    {
                        int typeIndex = photoProperty.Value.IndexOf("TYPE=JPEG;");
                        string encodedImage = (typeIndex < 0)
                            ? photoProperty.Value
                            : photoProperty.Value.Remove(typeIndex, "TYPE=JPEG;".Length);

                        int typeAltIndex = encodedImage.IndexOf("TYPE=JPEG:");
                        encodedImage = (typeAltIndex < 0)
                            ? encodedImage
                            : encodedImage.Remove(typeAltIndex, "TYPE=JPEG:".Length);

                        int encodingIndex = encodedImage.IndexOf("ENCODING=BASE64;");
                        encodedImage = (encodingIndex < 0)
                            ? encodedImage
                            : encodedImage.Remove(encodingIndex, "ENCODING=BASE64;".Length);

                        int encodingAltIndex = encodedImage.IndexOf("ENCODING=BASE64:");
                        encodedImage = (encodingAltIndex < 0)
                            ? encodedImage
                            : encodedImage.Remove(encodingAltIndex, "ENCODING=BASE64:".Length);

                        return Convert.FromBase64String(encodedImage);
                    }
                    else if (Manager.HttpClientFactory != null)
                    {
                        return Task.Run(() => WebRequests.DownloadImageFromURLAsync(photoProperty.Value, Manager.HttpClientFactory)).Result;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to convert vCard photo: {e.Message}", "ERROR");
                    return defaultImage;
                }
            }

            return defaultImage;
        }

        private static EmailAddress ConvertVCardEmail(EMailProperty emailProperty)
        {
            EmailAddress emailAddress = new() { Email = emailProperty.Value };

            emailAddress.Type = emailProperty.EMailTypes switch
            {
                EMailTypes.Preferred => (EmailAddress.EmailType?)EmailAddress.EmailType.Home,
                EMailTypes.None => (EmailAddress.EmailType?)EmailAddress.EmailType.None,
                EMailTypes.Internet => (EmailAddress.EmailType?)EmailAddress.EmailType.Internet,
                EMailTypes.AOL => (EmailAddress.EmailType?)EmailAddress.EmailType.AOL,
                EMailTypes.AppleLink => (EmailAddress.EmailType?)EmailAddress.EmailType.Applelink,
                EMailTypes.IBMMail => (EmailAddress.EmailType?)EmailAddress.EmailType.IBMMail,
                _ => (EmailAddress.EmailType?)EmailAddress.EmailType.Work,
            };

            return emailAddress;
        }

        private static PhoneNumber ConvertVCardPhone(TelephoneProperty phoneProperty)
        {
            PhoneNumber phoneNumber = new() { Number = phoneProperty.Value };

            phoneNumber.Type = phoneProperty.PhoneTypes switch
            {
                PhoneTypes.None => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.None,
                PhoneTypes.Work => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Work,
                PhoneTypes.Home or PhoneTypes.Preferred => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Home,
                PhoneTypes.Voice => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Voice,
                PhoneTypes.Fax => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Fax,
                PhoneTypes.Cell => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Cell,
                PhoneTypes.Pager => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Pager,
                PhoneTypes.BBS => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.BBS,
                PhoneTypes.Modem => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Modem,
                PhoneTypes.Car => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Car,
                PhoneTypes.ISDN => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.ISDN,
                PhoneTypes.Video => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Video,
                PhoneTypes.Text => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.Text,
                PhoneTypes.TextPhone => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.TextPhone,
                _ => (PhoneNumber.PhoneNumberType?)PhoneNumber.PhoneNumberType.None,
            };

            return phoneNumber;
        }

        private static void SetAddressFields(Contact contact, AddressPropertyCollection addresses)
        {
            if (addresses != null && addresses.Any())
            {
                AddressProperty addressProperty = addresses.OrderBy(a => a.PreferredOrder).First();

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
