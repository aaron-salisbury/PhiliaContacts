using PhiliaContacts.Domains;
using System;
using System.Text;

namespace PhiliaContacts.Core
{
    public class Writer
    {
        private const string HEADER = "BEGIN:VCARD";
        private const string FOOTER = "END:VCARD";
        private const string VERSION = "VERSION:3.0";

        public static string Write(Manager manager)
        {
            try
            {
                manager.Logger.Information("Beginning to write contacts.");

                StringBuilder stringBuilder = new StringBuilder();
                int itemCounter = 0;

                foreach (Contact contact in manager.Contacts)
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

                    foreach(EmailAddress email in contact.EmailAddresses)
                    {
                        if (email.Type == EmailAddress.EmailType.None)
                        {
                            stringBuilder.AppendLine(WrapLine($"EMAIL;TYPE=INTERNET:{email.Email}"));
                        }
                        else
                        {
                            stringBuilder.AppendLine(WrapLine($"EMAIL;TYPE=INTERNET;TYPE={email.Type.ToString().ToUpper()}:{email.Email}"));
                        }
                    }

                    foreach(PhoneNumber phone in contact.PhoneNumbers)
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

                    if (contact.Photo != null && contact.Photo != manager.DefaultContactImage)
                    {
                        stringBuilder.AppendLine(WrapLine($"PHOTO:TYPE=JPEG;ENCODING=BASE64:{Convert.ToBase64String(contact.Photo)}"));
                    }

                    stringBuilder.AppendLine(WrapLine(FOOTER));
                }

                if (stringBuilder.Length > 0)
                {
                    manager.Logger.Information("Successfully wrote contacts.");
                    return stringBuilder.ToString();
                }
                else
                {
                    manager.Logger.Warning("No contacts to write.");
                    return null;
                }
            }
            catch (Exception e)
            {
                manager.Logger.Error($"Failed to write contacts: {e.Message}");
                return null;
            }
        }

        private static string WrapLine(string line)
        {
            const int MAX_LENGTH = 75;

            StringBuilder builder = new StringBuilder();

            int counter = 0;
            foreach(char c in line)
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
