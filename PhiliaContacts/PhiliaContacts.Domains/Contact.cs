using PhiliaContacts.Domains.Base;
using System;
using System.Collections.ObjectModel;

namespace PhiliaContacts.Domains
{
    [Serializable]
    public class Contact : ObservableObject
    {
        private string _givenName;
        public string GivenName
        {
            get => _givenName;
            set
            {
                _givenName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FormattedName));
            }
        }

        public string MiddleName { get; set; }

        private string _familyName;
        public string FamilyName
        {
            get => _familyName;
            set
            {
                _familyName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FormattedName));
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        private string _nickname;
        public string Nickname
        {
            get => _nickname;
            set
            {
                _nickname = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        public string Prefix { get; set; }

        public string Suffix { get; set; }

        public string FormattedName => $"{GivenName} {FamilyName}";

        public string DisplayName => !string.IsNullOrEmpty(Nickname) ? Nickname : FamilyName;

        public ObservableCollection<EmailAddress> EmailAddresses { get; set; } = new ObservableCollection<EmailAddress>();

        public ObservableCollection<PhoneNumber> PhoneNumbers { get; set; } = new ObservableCollection<PhoneNumber>();

        private DateTime? _birthday;
        public DateTime? Birthday
        {
            get => _birthday;
            set
            {
                _birthday = value;
                RaisePropertyChanged();
            }
        }

        public string Title { get; set; }

        public string Organization { get; set; }

        public byte[] Photo { get; set; }

        public string TwitterUser { get; set; }

        public string FacebookUser { get; set; }

        public string LinkedInUser { get; set; }

        public ObservableCollection<Address> Addresses { get; set; } = new ObservableCollection<Address>();

        public string Url { get; set; }

        public string Notes { get; set; }

        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }

        public string CountryRegion { get; set; }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FavoriteSegoeMDL2Glyph));
            }
        }

        public string FavoriteSegoeMDL2Glyph => IsFavorite ? "\uE735" : "\uE734";

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Nickname))
            {
                return FormattedName;
            }

            return $"{FormattedName} ({Nickname})";
        }
    }
}
