using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PhiliaContacts.Business;
using PhiliaContacts.Business.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static PhiliaContacts.Business.Models.EmailAddress;
using static PhiliaContacts.Business.Models.PhoneNumber;

namespace PhiliaContacts.Presentation.Base.Controls
{
    public partial class ContactDetailsControl : UserControl
    {
        public IEnumerable<PhoneNumberType> PhoneNumberTypes { get; } = Enum.GetValues(typeof(PhoneNumberType)).Cast<PhoneNumberType>();
        public IEnumerable<EmailType> EmailTypes { get; } = Enum.GetValues(typeof(EmailType)).Cast<EmailType>();
        public IEnumerable<Contact.AddressTypes> AddressTypes { get; } = Enum.GetValues(typeof(Contact.AddressTypes)).Cast<Contact.AddressTypes>();
        public IEnumerable<string> CountryNames { get; } = Manager.CountryNames;

        private DataGrid? _emailGrid = null;
        private DataGrid? _phoneGrid = null;

        public ContactDetailsControl()
        {
            InitializeComponent();

            _emailGrid = this.FindControl<DataGrid>("EmailGrid");
            _phoneGrid = this.FindControl<DataGrid>("PhoneGrid");
        }

        private async Task<byte[]?> SelectPhotoAsync()
        {
            byte[]? selectedPhotoData = null;
            TopLevel? topLevel = TopLevel.GetTopLevel(this);

            if (topLevel != null)
            {
                IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Contact Photo",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>() { FilePickerFileTypes.ImageAll },
                    SuggestedStartLocation = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Pictures)
                });

                if (files.Count >= 1)
                {
                    using MemoryStream memoryStream = new();
                    await using Stream fileStream = await files[0].OpenReadAsync();
                    fileStream.CopyTo(memoryStream);

                    selectedPhotoData = memoryStream.ToArray();
                }
            }

            return selectedPhotoData;
        }

        #region Button Click Handlers
        private void AddEmailBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_emailGrid != null && DataContext is Contact contact)
            {
                EmailAddress newEmailAddress = new() { Type = EmailType.None };

                contact.EmailAddresses.Add(newEmailAddress);

                // Only resetting collection for change notification.
                contact.EmailAddresses = new ObservableCollection<EmailAddress>(contact.EmailAddresses);

                _emailGrid.SelectedItem = newEmailAddress;
            }
        }

        private void DeleteEmailBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_emailGrid?.SelectedItem != null && DataContext is Contact contact)
            {
                contact.EmailAddresses.Remove((EmailAddress)_emailGrid.SelectedItem);

                // Only resetting collection for change notification.
                contact.EmailAddresses = new ObservableCollection<EmailAddress>(contact.EmailAddresses);

                _emailGrid.SelectedItem = null;
            }
        }

        private void AddPhoneBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_phoneGrid != null && DataContext is Contact contact)
            {
                PhoneNumber newPhoneNumber = new() { Type = PhoneNumberType.None };

                contact.PhoneNumbers.Add(newPhoneNumber);

                // Only resetting collection for change notification.
                contact.PhoneNumbers = new ObservableCollection<PhoneNumber>(contact.PhoneNumbers);

                _phoneGrid.SelectedItem = newPhoneNumber;
            }
        }

        private void DeletePhoneBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_phoneGrid?.SelectedItem != null && DataContext is Contact contact)
            {
                contact.PhoneNumbers.Remove((PhoneNumber)_phoneGrid.SelectedItem);

                // Only resetting collection for change notification.
                contact.PhoneNumbers = new ObservableCollection<PhoneNumber>(contact.PhoneNumbers);

                _phoneGrid.SelectedItem = null;
            }
        }

        private void BirthdayClearBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is Contact contact)
            {
                contact.Birthday = null;
            }
        }

        private void FavoriteBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is Contact contact)
            {
                contact.IsFavorite = !contact.IsFavorite;
            }
        }

        private async void AddPhotoBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is Contact contact)
            {
                byte[]? photoImageData = await SelectPhotoAsync();

                if (photoImageData != null)
                {
                    contact.Photo = photoImageData;
                }
            }
        }
        #endregion
    }
}
