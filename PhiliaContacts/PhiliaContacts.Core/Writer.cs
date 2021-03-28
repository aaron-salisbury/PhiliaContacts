using PhiliaContacts.Domains;
using System;
using System.Text;

namespace PhiliaContacts.Core
{
    public class Writer
    {
        private const string HEADER = "BEGIN:VCARD";
        private const string FOOTER = "END:VCARD";
        private const string VERSION = "3.0";
        public static string Write(Manager manager)
        {
            try
            {
                manager.Logger.Information("Beginning to write contacts.");

                string rev = "2021-03-26T17:56:19Z"; //TODO: get this format from DateTime.Now.

                StringBuilder stringBuilder = new StringBuilder();

                foreach (Contact contact in manager.Contacts)
                {
                    //TODO: Limit line length. Wrap and intent one space.
                    //TODO: Don't try to write valueless lines.

                    stringBuilder.Append(HEADER + Environment.NewLine + VERSION + Environment.NewLine);

                    stringBuilder.Append($"N:{contact.FamilyName};{contact.GivenName};{contact.MiddleName};;");
                    stringBuilder.Append(Environment.NewLine);



                    if (!string.IsNullOrEmpty(contact.FormattedName))
                    {
                        stringBuilder.Append($"FN:{contact.FormattedName}");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(contact.Organization))
                    {
                        stringBuilder.Append($"ORG:{contact.Organization};");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(contact.Organization))
                    {
                        stringBuilder.Append($"ORG:{contact.Organization};");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(contact.Nickname))
                    {
                        stringBuilder.Append($"NICKNAME:{contact.Nickname}");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (contact.Birthday != null)
                    {
                        stringBuilder.Append($"BDAY:{contact.Birthday.Value.ToString("yyyy’-‘MM’-‘dd")}");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(contact.Title))
                    {
                        stringBuilder.Append($"TITLE:{contact.Title}");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(contact.Notes))
                    {
                        stringBuilder.Append($"NOTE:{contact.Notes}");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    //TODO: Address.



                    if (!string.IsNullOrEmpty(contact.TwitterUser))
                    {
                        stringBuilder.Append($"X-SOCIALPROFILE;X-USER={contact.TwitterUser};TYPE=twitter:http://twitter.com/" + contact.TwitterUser);
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(contact.FacebookUser))
                    {
                        stringBuilder.Append($"X-SOCIALPROFILE;X-USER={contact.FacebookUser};TYPE=facebook:http://facebook.com/" + contact.FacebookUser);
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrEmpty(contact.LinkedInUser))
                    {
                        stringBuilder.Append($"X-SOCIALPROFILE;X-USER={contact.LinkedInUser};TYPE=linkedin:http://www.linkedin.com/in/" + contact.LinkedInUser);
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (contact.IsFavorite)
                    {
                        stringBuilder.Append("CATEGORIES:starred");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    if (contact.Photo != null && contact.Photo != manager.DefaultContactImage) //TODO: and not default
                    {
                        stringBuilder.Append($"PHOTO:TYPE=JPEG;ENCODING=BASE64:{Convert.ToBase64String(contact.Photo, 0, contact.Photo.Length)}");
                        stringBuilder.Append(Environment.NewLine);
                    }

                    stringBuilder.Append(rev + Environment.NewLine + FOOTER + Environment.NewLine);
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
