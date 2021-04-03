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

                string rev = "REV:" + "2021-03-26T17:56:19Z"; //TODO: get this format from DateTime.Now.

                StringBuilder stringBuilder = new StringBuilder();

                foreach (Contact contact in manager.Contacts)
                {
                    if (!contact.IsValid)
                    {
                        continue;
                    }

                    //TODO: Limit line length. Wrap and intent one space.
                    //TODO: Don't try to write valueless lines.

                    stringBuilder.AppendLine(HEADER + Environment.NewLine + VERSION);

                    stringBuilder.AppendLine($"N:{contact.FamilyName};{contact.GivenName};{contact.MiddleName};;");

                    if (!string.IsNullOrEmpty(contact.FormattedName))
                    {
                        stringBuilder.AppendLine($"FN:{contact.FormattedName}");
                    }

                    if (!string.IsNullOrEmpty(contact.Organization))
                    {
                        stringBuilder.AppendLine($"ORG:{contact.Organization};");
                    }

                    if (!string.IsNullOrEmpty(contact.Nickname))
                    {
                        stringBuilder.AppendLine($"NICKNAME:{contact.Nickname}");
                    }

                    if (contact.Birthday != null)
                    {
                        stringBuilder.AppendLine($"BDAY;VALUE=date:{contact.Birthday.Value:yyyy-MM-dd}");
                    }

                    if (!string.IsNullOrEmpty(contact.Title))
                    {
                        stringBuilder.AppendLine($"TITLE:{contact.Title}");
                    }

                    if (!string.IsNullOrEmpty(contact.Notes))
                    {
                        stringBuilder.AppendLine($"NOTE:{contact.Notes}");
                    }

                    //TODO: Address, phones, URL, emails. Also don't forget about IsFavorite, URL, and any others.



                    if (!string.IsNullOrEmpty(contact.TwitterUser))
                    {
                        stringBuilder.AppendLine($"X-SOCIALPROFILE;X-USER={contact.TwitterUser};TYPE=twitter:http://twitter.com/" + contact.TwitterUser);
                    }

                    if (!string.IsNullOrEmpty(contact.FacebookUser))
                    {
                        stringBuilder.AppendLine($"X-SOCIALPROFILE;X-USER={contact.FacebookUser};TYPE=facebook:http://facebook.com/" + contact.FacebookUser);
                    }

                    if (!string.IsNullOrEmpty(contact.LinkedInUser))
                    {
                        stringBuilder.AppendLine($"X-SOCIALPROFILE;X-USER={contact.LinkedInUser};TYPE=linkedin:http://www.linkedin.com/in/" + contact.LinkedInUser);
                    }

                    if (contact.IsFavorite)
                    {
                        stringBuilder.AppendLine("CATEGORIES:starred");
                    }

                    if (contact.Photo != null && contact.Photo != manager.DefaultContactImage)
                    {
                        //stringBuilder.AppendLine($"PHOTO:TYPE=JPEG;ENCODING=BASE64:{Convert.ToBase64String(contact.Photo, 0, contact.Photo.Length)}");

                        //stringBuilder.AppendLine("PHOTO;ENCODING=BASE64;TYPE=JPEG:");
                        //stringBuilder.AppendLine(Convert.ToBase64String(contact.Photo));
                        //stringBuilder.AppendLine(string.Empty);

                        stringBuilder.AppendLine($"PHOTO:TYPE=JPEG;ENCODING=BASE64:{Convert.ToBase64String(contact.Photo)}");
                    }

                    stringBuilder.AppendLine(rev + Environment.NewLine + FOOTER);
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
    }
}
