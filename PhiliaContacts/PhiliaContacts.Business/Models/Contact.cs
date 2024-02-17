using PhiliaContacts.Business.Base;
using System.Collections.ObjectModel;

namespace PhiliaContacts.Business.Models
{
    public class Contact : ObservableModel
    {
        private string? _givenName;
        public string? GivenName
        {
            get => _givenName;
            set
            {
                if (SetProperty(ref _givenName, value))
                {
                    RaisePropertyChanged(nameof(FormattedName));
                }
            }
        }

        private string? _middleName;
        public string? MiddleName
        {
            get => _middleName;
            set => SetProperty(ref _middleName, value);
        }

        private string? _familyName;
        public string? FamilyName
        {
            get => _familyName;
            set
            {
                if (SetProperty(ref _familyName, value))
                {
                    RaisePropertyChanged(nameof(FormattedName));
                    RaisePropertyChanged(nameof(DisplayName));
                }
            }
        }

        private string? _nickname;
        public string? Nickname
        {
            get => _nickname;
            set
            {
                if (SetProperty(ref _nickname, value))
                {
                    RaisePropertyChanged(nameof(DisplayName));
                }
            }
        }

        private string? _prefix;
        public string? Prefix
        {
            get => _prefix;
            set => SetProperty(ref _prefix, value);
        }

        private string? _suffix;
        public string? Suffix
        {
            get => _suffix;
            set => SetProperty(ref _suffix, value);
        }

        private DateTime? _birthday = null;
        public DateTime? Birthday
        {
            get => _birthday;
            set => SetProperty(ref _birthday, value);
        }

        private string? _title;
        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string? _organization;
        public string? Organization
        {
            get => _organization;
            set => SetProperty(ref _organization, value);
        }

        private byte[]? _photo;
        public byte[]? Photo
        {
            get => _photo;
            set => SetProperty(ref _photo, value);
        }

        private string? _twitterUser;
        public string? TwitterUser
        {
            get => _twitterUser;
            set => SetProperty(ref _twitterUser, value);
        }

        private string? _facebookUser;
        public string? FacebookUser
        {
            get => _facebookUser;
            set => SetProperty(ref _facebookUser, value);
        }

        private string? _linkedInUser;
        public string? LinkedInUser
        {
            get => _linkedInUser;
            set => SetProperty(ref _linkedInUser, value);
        }

        private string? _url;
        public string? Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        private string? _notes;
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private string? _street;
        public string? Street
        {
            get => _street;
            set => SetProperty(ref _street, value);
        }

        private string? _city;
        public string? City
        {
            get => _city;
            set => SetProperty(ref _city, value);
        }

        private string? _state;
        public string? State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        private string? _zip;
        public string? Zip
        {
            get => _zip;
            set => SetProperty(ref _zip, value);
        }

        private string? _countryRegion;
        public string? CountryRegion
        {
            get => _countryRegion;
            set => SetProperty(ref _countryRegion, value);
        }

        private AddressTypes _addressType = AddressTypes.None;
        public AddressTypes AddressType
        {
            get => _addressType;
            set => SetProperty(ref _addressType, value);
        }

        private ObservableCollection<PhoneNumber> _phoneNumbers = new();
        public ObservableCollection<PhoneNumber> PhoneNumbers
        {
            get => _phoneNumbers;
            set => SetProperty(ref _phoneNumbers, value);
        }

        private ObservableCollection<EmailAddress> _emailAddresses = new();
        public ObservableCollection<EmailAddress> EmailAddresses
        {
            get => _emailAddresses;
            set => SetProperty(ref _emailAddresses, value);
        }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (SetProperty(ref _isFavorite, value))
                {
                    RaisePropertyChanged(nameof(FavoriteSegoeMDL2Glyph));
                }
            }
        }

        public string FavoriteSegoeMDL2Glyph => IsFavorite ? "\uE735" : "\uE734";

        public string FormattedName => $"{GivenName} {FamilyName}";

        public string? DisplayName => !string.IsNullOrEmpty(Nickname) ? Nickname : FamilyName;

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(GivenName) && string.IsNullOrEmpty(FamilyName) && string.IsNullOrEmpty(Nickname))
                {
                    return false;
                }

                return true;
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Nickname))
            {
                return FormattedName;
            }

            return $"{FormattedName} ({Nickname})";
        }

        public enum AddressTypes
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
