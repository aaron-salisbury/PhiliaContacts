using PhiliaContacts.Business.Base.Extensions;
using PhiliaContacts.Business.Models;
using PhiliaContacts.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using vCardLib.Deserializers;

namespace PhiliaContacts.Business.Services.VirtualContactFile
{
    public class VCardLibVCFService : IVCFParserService
    {
        public IEnumerable<Contact> GetContactsFromVCFContents(string vcfContents)
        {
            List<Contact> contacts = new();

            try
            {
                Debug.WriteLine("Beginning to convert vCard file.", "INFO");

                byte[]? placeholderImage = Access.ReadContactImage();

                foreach (vCardLib.Models.vCard vCard in Deserializer.FromString(vcfContents))
                {
                    Contact newContact = new()
                    {
                        GivenName = vCard.GivenName,
                        MiddleName = vCard.MiddleName,
                        FamilyName = vCard.FamilyName,
                        Nickname = vCard.NickName,
                        Suffix = vCard.Suffix,
                        Prefix = vCard.Prefix,
                        EmailAddresses = new ObservableCollection<EmailAddress>(vCard.EmailAddresses.Select(ea => ConvertVCardEmail(ea))),
                        PhoneNumbers = new ObservableCollection<PhoneNumber>(vCard.PhoneNumbers.Select(pn => ConvertVCardPhone(pn))),
                        Title = vCard.Title,
                        Photo = vCard.Pictures?.Where(p => p.Picture != null)?.FirstOrDefault()?.Picture ?? placeholderImage,
                        Notes = vCard.Note,
                        Birthday = vCard.BirthDay,
                        IsFavorite = vCard.CustomFields?.Any(kvp => string.Equals(kvp.Key, "CATEGORIES") && kvp.Value.Contains("starred")) ?? false,
                        Organization = !string.IsNullOrEmpty(vCard.Organization) && vCard.Organization.Contains(";") ?
                            vCard.Organization.GetUntilOrEmpty(";") :
                            vCard.Organization,
                    };

                    if (vCard.CustomFields != null)
                    {
                        if (vCard.CustomFields.Any(cf => cf.Key.Contains("URL;")))
                        {
                            newContact.Url = vCard.CustomFields
                                .Where(cf => !string.IsNullOrEmpty(cf.Key) && cf.Key.Contains(".URL;"))?
                                .FirstOrDefault().Value;
                        }

                        if (vCard.CustomFields.Any(cf => cf.Key.EndsWith("TYPE=twitter")))
                        {
                            newContact.TwitterUser = vCard.CustomFields
                                .Where(cf => !string.IsNullOrEmpty(cf.Key) && cf.Key.EndsWith("TYPE=twitter"))?
                                .FirstOrDefault().Key
                                .GetAfterLastOrEmpty("USER=")
                                .GetUntilOrEmpty(";");
                        }

                        if (vCard.CustomFields.Any(cf => cf.Key.EndsWith("TYPE=facebook")))
                        {
                            newContact.FacebookUser = vCard.CustomFields
                                .Where(cf => !string.IsNullOrEmpty(cf.Key) && cf.Key.EndsWith("TYPE=facebook"))?
                                .FirstOrDefault().Key
                                .GetAfterLastOrEmpty("USER=")
                                .GetUntilOrEmpty(";");
                        }

                        if (vCard.CustomFields.Any(cf => cf.Key.EndsWith("TYPE=linkedin")))
                        {
                            newContact.LinkedInUser = vCard.CustomFields
                                .Where(cf => !string.IsNullOrEmpty(cf.Key) && cf.Key.EndsWith("TYPE=linkedin"))?
                                .FirstOrDefault().Key
                                .GetAfterLastOrEmpty("USER=")
                                .GetUntilOrEmpty(";");
                        }

                        if (vCard.CustomFields.Any(cf => cf.Key.StartsWith("item") && cf.Key.Contains(".ADR;")))
                        {
                            List<KeyValuePair<string, string>> addressProperties = vCard.CustomFields
                                .Where(cf => cf.Key.StartsWith("item") && cf.Key.Contains(".ADR;"))
                                .ToList();

                            KeyValuePair<string, string> addressProperty;

                            if (addressProperties.Any(ap => ap.Key.Contains("TYPE=pref")))
                            {
                                addressProperty = addressProperties
                                .Where(ap => ap.Key.Contains("TYPE=pref"))
                                .FirstOrDefault();
                            }
                            else
                            {
                                addressProperty = addressProperties.FirstOrDefault();
                            }

                            string[] addressParts = addressProperty.Value.Split(';');

                            newContact.Street = addressParts.Length >= 3 && !string.IsNullOrEmpty(addressParts[2]) ? addressParts[2] : null;
                            newContact.City = addressParts.Length >= 4 && !string.IsNullOrEmpty(addressParts[3]) ? addressParts[3] : null;
                            newContact.State = addressParts.Length >= 5 && !string.IsNullOrEmpty(addressParts[4]) ? addressParts[4] : null;
                            newContact.Zip = addressParts.Length >= 6 && !string.IsNullOrEmpty(addressParts[5]) ? addressParts[5] : null;
                        }
                    }

                    contacts.Add(newContact);
                }

                Debug.WriteLine("Successfully converted vCard file.", "INFO");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to convert vCard file: {ex.Message}", "ERROR");
                contacts = new List<Contact>();
            }

            return contacts;
        }

        private static EmailAddress ConvertVCardEmail(vCardLib.Models.EmailAddress vCardEmail)
        {
            return new EmailAddress
            {
                Email = vCardEmail.Value.Contains(":") ? vCardEmail.Value.GetAfterLastOrEmpty(":") : vCardEmail.Value,
                Type = (EmailAddress.EmailType)Convert.ToInt32(vCardEmail.Type)
            };
        }

        private static PhoneNumber ConvertVCardPhone(vCardLib.Models.TelephoneNumber vCardPhone)
        {
            return new PhoneNumber
            {
                Number = vCardPhone.Value,
                Type = (PhoneNumber.PhoneNumberType)Convert.ToInt32(vCardPhone.Type)
            };
        }
    }
}
