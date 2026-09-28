using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PhiliaContacts.Presentation.Desktop.ViewModels;

public partial class EditableValue : ObservableObject
{
    [ObservableProperty] private string _value = string.Empty;
    [ObservableProperty] private string _type = "Home";

    public EditableValue() { }

    public EditableValue(ContactValue value)
    {
        Value = value.Value;
        Type = value.Type;
    }
}

public partial class EditableAddress : ObservableObject
{
    [ObservableProperty] private string _type = "Home";
    [ObservableProperty] private string? _street;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _region;
    [ObservableProperty] private string? _postalCode;
    [ObservableProperty] private string? _country;

    public EditableAddress() { }

    public EditableAddress(ContactAddress address)
    {
        Type = address.Type;
        Street = address.Street;
        City = address.City;
        Region = address.Region;
        PostalCode = address.PostalCode;
        Country = address.Country;
    }
}

public partial class ContactEditorViewModel : ObservableObject, IDisposable
{
    private readonly ContactId _id;
    private readonly string[] _vCardProperties = [];
    private byte[]? _photo;
    private bool _disposed;

    [ObservableProperty] private string? _givenName;
    [ObservableProperty] private string? _middleName;
    [ObservableProperty] private string? _familyName;
    [ObservableProperty] private string? _phoneticGivenName;
    [ObservableProperty] private string? _phoneticFamilyName;
    [ObservableProperty] private string? _nickname;
    [ObservableProperty] private string? _prefix;
    [ObservableProperty] private string? _suffix;
    [ObservableProperty] private string? _birthday;
    [ObservableProperty] private string? _title;
    [ObservableProperty] private string? _organization;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _twitterUser;
    [ObservableProperty] private string? _facebookUser;
    [ObservableProperty] private string? _linkedInUser;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private EditableValue? _selectedPhone;
    [ObservableProperty] private EditableValue? _selectedEmail;
    [ObservableProperty] private EditableAddress? _selectedAddress;
    [ObservableProperty] private Bitmap? _photoPreview;
    [ObservableProperty] private string _photoDescription = "No photo";

    public ObservableCollection<EditableValue> PhoneNumbers { get; } = [];
    public ObservableCollection<EditableValue> EmailAddresses { get; } = [];
    public ObservableCollection<EditableAddress> Addresses { get; } = [];

    public ContactEditorViewModel(Contact? contact = null)
    {
        _id = contact?.Id ?? ContactId.New();

        if (contact is null)
        {
            return;
        }

        GivenName = contact.GivenName;
        MiddleName = contact.MiddleName;
        FamilyName = contact.FamilyName;
        PhoneticGivenName = contact.PhoneticGivenName;
        PhoneticFamilyName = contact.PhoneticFamilyName;
        Nickname = contact.Nickname;
        Prefix = contact.Prefix;
        Suffix = contact.Suffix;
        Birthday = contact.Birthday;
        Title = contact.Title;
        Organization = contact.Organization;
        Url = contact.Url;
        Notes = contact.Notes;
        TwitterUser = contact.TwitterUser;
        FacebookUser = contact.FacebookUser;
        LinkedInUser = contact.LinkedInUser;
        IsFavorite = contact.IsFavorite;
        _vCardProperties = [.. contact.VCardProperties];

        foreach (ContactValue value in contact.PhoneNumbers)
        {
            PhoneNumbers.Add(new(value));
        }

        foreach (ContactValue value in contact.EmailAddresses)
        {
            EmailAddresses.Add(new(value));
        }

        foreach (ContactAddress address in contact.Addresses)
        {
            Addresses.Add(new(address));
        }

        SetPhoto(contact.Photo);
    }

    public void SetPhoto(byte[]? bytes)
    {
        if (bytes?.Length > 8 * 1024 * 1024)
        {
            throw new InvalidDataException("Photos must be 8 MiB or smaller.");
        }

        Bitmap? preview = null;
        if (bytes is not null)
        {
            if (!IsJpeg(bytes) && !IsPng(bytes))
            {
                throw new InvalidDataException("Select a JPEG or PNG photo.");
            }

            try
            {
                using MemoryStream stream = new(bytes);
                preview = new Bitmap(stream);
            }
            catch
            {
                // Keep the original bytes even if the platform cannot preview the image.
            }
        }

        Bitmap? previous = PhotoPreview;
        _photo = bytes is null ? null : [.. bytes];
        PhotoPreview = preview;
        PhotoDescription = bytes is null ? "No photo" : preview is null ? "Photo saved; preview unavailable" :
            $"{(IsPng(bytes) ? "PNG" : "JPEG")} photo ({bytes.Length / 1024} KiB)";

        previous?.Dispose();
    }

    private static bool IsPng(byte[] data)
    {
        return data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
    }

    private static bool IsJpeg(byte[] data)
    {
        return data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff;
    }

    public Contact ToContact()
    {
        return new Contact
        {
            Id = _id,
            GivenName = GivenName,
            MiddleName = MiddleName,
            FamilyName = FamilyName,
            PhoneticGivenName = PhoneticGivenName,
            PhoneticFamilyName = PhoneticFamilyName,
            Nickname = Nickname,
            Prefix = Prefix,
            Suffix = Suffix,
            Birthday = Birthday,
            Title = Title,
            Organization = Organization,
            Url = Url,
            Notes = Notes,
            TwitterUser = TwitterUser,
            FacebookUser = FacebookUser,
            LinkedInUser = LinkedInUser,
            IsFavorite = IsFavorite,
            Photo = _photo is null ? null : [.. _photo],
            PhoneNumbers = [.. PhoneNumbers.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => new ContactValue(x.Value.Trim(), x.Type.Trim()))],
            EmailAddresses = [.. EmailAddresses.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => new ContactValue(x.Value.Trim(), x.Type.Trim()))],
            Addresses = [.. Addresses.Select(x => new ContactAddress(x.Type.Trim(), x.Street, x.City, x.Region, x.PostalCode, x.Country))],
            VCardProperties = [.. _vCardProperties]
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            PhotoPreview?.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
