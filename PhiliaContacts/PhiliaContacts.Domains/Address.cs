using System;

namespace PhiliaContacts.Domains
{
    [Serializable]
    public class Address
    {
        //TODO: Break up address.
        public string Location { get; set; }
        public AddressType Type { get; set; }

        public enum AddressType
        {
            Work = 0,
            Home = 1,
            Domestic = 2,
            International = 3,
            Postal = 4,
            Parcel = 5,
            None = 6
        }
    }
}
