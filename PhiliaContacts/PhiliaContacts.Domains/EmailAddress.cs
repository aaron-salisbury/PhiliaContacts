using System;

namespace PhiliaContacts.Domains
{
    [Serializable]
    public class EmailAddress
    {
        public string Email { get; set; }
        public EmailType Type { get; set; }

        public enum EmailType
        {
            Work = 0,
            Internet = 1,
            Home = 2,
            AOL = 3,
            Applelink = 4,
            IBMMail = 5,
            None = 6
        }
    }
}
