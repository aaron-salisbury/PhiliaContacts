using System;

namespace PhiliaContacts.Domains
{
    [Serializable]
    public class PhoneNumber
    {
        public string Number { get; set; }
        public PhoneNumberType Type { get; set; }

        public enum PhoneNumberType
        {
            Work = 0,
            Cell = 1,
            Home = 2,
            Voice = 3,
            Text = 4,
            Fax = 5,
            Pager = 6,
            Video = 7,
            TextPhone = 8,
            MainNumber = 9,
            BBS = 10,
            Modem = 11,
            Car = 12,
            ISDN = 13,
            None = 14
        }
    }
}
