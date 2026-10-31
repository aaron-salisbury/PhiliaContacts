using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

public partial class ContactDetailViewModel : ObservableObject, IDisposable
{
    private readonly ContactId _id;
    private readonly string[] _vCardProperties = [];
    private byte[]? _photo;
    private bool _disposed;
    private bool _trackChanges;

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
    [ObservableProperty] private bool _isDirty;

    /// <summary>
    /// Raised with a user-facing message when an editor action (such as choosing a photo) fails.
    /// </summary>
    public event EventHandler<string>? MessageRaised;

    public ContactId Id => _id;

    public ObservableCollection<EditableValue> PhoneNumbers { get; } = [];
    public ObservableCollection<EditableValue> EmailAddresses { get; } = [];
    public ObservableCollection<EditableAddress> Addresses { get; } = [];

    public ContactDetailViewModel(Contact? contact = null)
    {
        _id = contact?.Id ?? ContactId.New();

        PhoneNumbers.CollectionChanged += Collection_CollectionChanged;
        EmailAddresses.CollectionChanged += Collection_CollectionChanged;
        Addresses.CollectionChanged += Collection_CollectionChanged;

        if (contact is null)
        {
            _trackChanges = true;
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

        LoadStoredPhoto(contact.Photo);
        _trackChanges = true;
    }

    /// <summary>
    /// Marks the current values as saved.
    /// </summary>
    public void AcceptChanges()
    {
        IsDirty = false;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_trackChanges && e.PropertyName is not (nameof(IsDirty) or nameof(SelectedPhone) or nameof(SelectedEmail) or nameof(SelectedAddress)
            or nameof(PhotoPreview) or nameof(PhotoDescription)))
        {
            IsDirty = true;
        }
    }

    private void Collection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (INotifyPropertyChanged item in e.NewItems?.OfType<INotifyPropertyChanged>() ?? [])
        {
            item.PropertyChanged += Item_PropertyChanged;
        }

        foreach (INotifyPropertyChanged item in e.OldItems?.OfType<INotifyPropertyChanged>() ?? [])
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }

        if (_trackChanges)
        {
            IsDirty = true;
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_trackChanges)
        {
            IsDirty = true;
        }
    }

    // A stored photo that cannot be previewed (e.g. an unsupported format from older data) must not break opening the contact;
    // the bytes are kept as they are so saving does not discard them.
    private void LoadStoredPhoto(byte[]? bytes)
    {
        try
        {
            SetPhoto(bytes);
        }
        catch (InvalidDataException)
        {
            _photo = bytes is null ? null : [.. bytes];
            PhotoPreview = null;
            PhotoDescription = "Photo format not supported for preview";
        }
    }

    [RelayCommand]
    private void FlipFavorite()
    {
        IsFavorite = !IsFavorite;
    }

    [RelayCommand]
    private void AddPhone()
    {
        PhoneNumbers.Add(new EditableValue());
    }

    [RelayCommand]
    private void RemovePhone()
    {
        if (SelectedPhone is { } item)
        {
            PhoneNumbers.Remove(item);
        }
    }

    [RelayCommand]
    private void AddEmail()
    {
        EmailAddresses.Add(new EditableValue());
    }

    [RelayCommand]
    private void RemoveEmail()
    {
        if (SelectedEmail is { } item)
        {
            EmailAddresses.Remove(item);
        }
    }

    [RelayCommand]
    private void AddAddress()
    {
        Addresses.Add(new EditableAddress());
    }

    [RelayCommand]
    private void RemoveAddress()
    {
        if (SelectedAddress is { } item)
        {
            Addresses.Remove(item);
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        if (_photo is not null)
        {
            SetPhoto(null);
            IsDirty = true;
        }
    }

    public async Task SetPhotoAsync(Stream stream)
    {
        try
        {
            using MemoryStream buffer = new();
            await stream.CopyToAsync(buffer);
            SetPhoto(buffer.ToArray());
            IsDirty = true;
            MessageRaised?.Invoke(this, "Photo selected. Save the contact to keep it.");
        }
        catch (Exception error)
        {
            MessageRaised?.Invoke(this, "Could not use photo: " + error.Message);
        }
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
