using PhiliaContacts.Business.Base;

namespace PhiliaContacts.Business.Models
{
    public class EmailAddress : ObservableModel
    {
        private string _email = null!;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private EmailType? _type;
        public EmailType? Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

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
