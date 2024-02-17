using PhiliaContacts.Business.Models;
using System.Diagnostics;
using System.Text;

namespace PhiliaContacts.Business
{
    public static class Exporter
    {
        private const string HEADER = "BEGIN:VCARD";
        private const string FOOTER = "END:VCARD";
        private const string VERSION = "VERSION:3.0";

        public static string? Export(IEnumerable<Contact> contacts)
        {
            // https://weblogs.asp.net/gunnarpeipman/creating-vcard-with-image-in-net

            try
            {
                Debug.WriteLine("Beginning to write contacts.", "INFO");

                StringBuilder stringBuilder = new();
                int itemCounter = 0;

                foreach (Contact contact in contacts)
                {
                    if (!contact.IsValid)
                    {
                        continue;
                    }

                    stringBuilder.AppendLine(WrapLine(HEADER + Environment.NewLine + VERSION));

                    stringBuilder.AppendLine(WrapLine($"N:{contact.FamilyName};{contact.GivenName};{contact.MiddleName};;"));

                    if (!string.IsNullOrEmpty(contact.FormattedName))
                    {
                        stringBuilder.AppendLine(WrapLine($"FN:{contact.FormattedName}"));
                    }

                    if (!string.IsNullOrEmpty(contact.Nickname))
                    {
                        stringBuilder.AppendLine(WrapLine($"NICKNAME:{contact.Nickname}"));
                    }

                    if (contact.IsFavorite)
                    {
                        stringBuilder.AppendLine(WrapLine("CATEGORIES:starred"));
                    }

                    if (contact.Birthday != null)
                    {
                        stringBuilder.AppendLine(WrapLine($"BDAY;VALUE=date:{contact.Birthday.Value:yyyy-MM-dd}"));
                    }

                    if (!string.IsNullOrEmpty(contact.Organization))
                    {
                        stringBuilder.AppendLine(WrapLine($"ORG:{contact.Organization};"));
                    }

                    if (!string.IsNullOrEmpty(contact.Title))
                    {
                        stringBuilder.AppendLine(WrapLine($"TITLE:{contact.Title}"));
                    }

                    if (!string.IsNullOrEmpty(contact.Url))
                    {
                        stringBuilder.AppendLine(WrapLine($"URL;TYPE=HOME:{contact.Url}"));
                    }

                    if (!string.IsNullOrEmpty(contact.Notes))
                    {
                        stringBuilder.AppendLine(WrapLine($"NOTE:{contact.Notes}"));
                    }

                    foreach (EmailAddress email in contact.EmailAddresses)
                    {
                        if (email.Type == EmailAddress.EmailType.None)
                        {
                            stringBuilder.AppendLine(WrapLine($"EMAIL;TYPE=INTERNET:{email.Email}"));
                        }
                        else if (email.Type != null)
                        {
                            string emailType = email.Type?.ToString() ?? string.Empty;
                            stringBuilder.AppendLine(WrapLine($"EMAIL;TYPE=INTERNET;TYPE={emailType.ToUpper()}:{email.Email}"));
                        }
                    }

                    foreach (PhoneNumber phone in contact.PhoneNumbers)
                    {
                        itemCounter++;
                        stringBuilder.AppendLine(WrapLine($"item{itemCounter}.TEL:{phone.Number}"));
                        stringBuilder.AppendLine(WrapLine($"item{itemCounter}.X-ABLabel:{phone.Type}"));
                    }

                    if (!string.IsNullOrEmpty(contact.Street))
                    {
                        stringBuilder.AppendLine(WrapLine($"ADR;TYPE={contact.AddressType.ToString().ToUpper()}:;;{contact.Street};{contact.City};{contact.State};{contact.Zip};{contact.CountryRegion}"));
                    }

                    if (!string.IsNullOrEmpty(contact.TwitterUser))
                    {
                        stringBuilder.AppendLine(WrapLine($"X-SOCIALPROFILE;X-USER={contact.TwitterUser};TYPE=twitter:http://twitter.com/" + contact.TwitterUser));
                    }

                    if (!string.IsNullOrEmpty(contact.FacebookUser))
                    {
                        stringBuilder.AppendLine(WrapLine($"X-SOCIALPROFILE;X-USER={contact.FacebookUser};TYPE=facebook:http://facebook.com/" + contact.FacebookUser));
                    }

                    if (!string.IsNullOrEmpty(contact.LinkedInUser))
                    {
                        stringBuilder.AppendLine(WrapLine($"X-SOCIALPROFILE;X-USER={contact.LinkedInUser};TYPE=linkedin:http://www.linkedin.com/in/" + contact.LinkedInUser));
                    }

                    if (contact.Photo != null && contact.Photo != Manager.DefaultUserImage)
                    {
                        stringBuilder.AppendLine(WrapLine($"PHOTO:TYPE=JPEG;ENCODING=BASE64:{Convert.ToBase64String(contact.Photo)}"));
                    }

                    stringBuilder.AppendLine(WrapLine(FOOTER));
                }

                if (stringBuilder.Length > 0)
                {
                    Debug.WriteLine("Successfully wrote contacts.", "INFO");
                    return stringBuilder.ToString();
                }
                else
                {
                    Debug.WriteLine("No contacts to write.", "WARN");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write contacts: {ex.Message}", "ERROR");
                return null;
            }
        }

        private static string WrapLine(string line)
        {
            const int MAX_LENGTH = 75;

            StringBuilder builder = new();

            int counter = 0;
            foreach (char c in line)
            {
                counter++;

                if (counter < MAX_LENGTH)
                {
                    builder.Append(c);
                }
                else if (counter == MAX_LENGTH)
                {
                    builder.AppendLine(c.ToString());
                }
                else if (counter > MAX_LENGTH)
                {
                    builder.Append(" " + c);
                    counter = 2;
                }
            }

            return builder.ToString();
        }
    }
}
