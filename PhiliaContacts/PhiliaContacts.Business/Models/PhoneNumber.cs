using PhiliaContacts.Business.Base;

namespace PhiliaContacts.Business.Models
{
    public class PhoneNumber : ObservableModel
    {
        private string _number = null!;
        public string Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }

        private PhoneNumberType? _type;
        public PhoneNumberType? Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

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
