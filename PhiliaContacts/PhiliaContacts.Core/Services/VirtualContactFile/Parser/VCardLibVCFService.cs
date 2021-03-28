using PhiliaContacts.Domains;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using vCardLib.Deserializers;

namespace PhiliaContacts.Core.Services
{
    public class VCardLibVCFService : IVCFParserService
    {
        private ILogger _logger { get; set; }

        public VCardLibVCFService(ILogger logger)
        {
            _logger = logger;
        }

        public IEnumerable<Contact> GetContactsFromVCFContents(string vcfContents)
        {
            try
            {
                List<Contact> contacts = new List<Contact>();

                _logger.Information("Beginning to convert vCard file.");

                foreach (vCardLib.Models.vCard vCard in Deserializer.FromString(vcfContents))
                {
                    Contact newContact = new Contact
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
                        Organization = vCard.Organization,
                        Photo = vCard.Pictures?.Where(p => p.Picture != null)?.FirstOrDefault()?.Picture,

                        //TODO: Twitter, Facebook, LinkedIn
                        
                        //TODO: Addresses

                        Url = vCard.Url,
                        Notes = vCard.Note,
                        IsFavorite = vCard.CustomFields?.Any(kvp => string.Equals(kvp.Key, "CATEGORIES") && kvp.Value.Contains("starred")) ?? false
                    };

                    if (vCard.BirthDay != null)
                    {
                        newContact.Birthday = vCard.BirthDay.Value;
                    }

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

        private static EmailAddress ConvertVCardEmail(vCardLib.Models.EmailAddress vCardEmail)
        {
            return new EmailAddress
            {
                Email = vCardEmail.Email,
                Type = (EmailAddress.EmailType)Convert.ToInt32(vCardEmail.Type)
            };
        }

        private static PhoneNumber ConvertVCardPhone(vCardLib.Models.PhoneNumber vCardPhone)
        {
            return new PhoneNumber
            {
                Number = vCardPhone.Number,
                Type = (PhoneNumber.PhoneNumberType)Convert.ToInt32(vCardPhone.Type)
            };
        }
    }
}
