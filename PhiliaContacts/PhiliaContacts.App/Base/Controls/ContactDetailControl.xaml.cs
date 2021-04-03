using PhiliaContacts.App.Base.Helpers;
using PhiliaContacts.Core;
using PhiliaContacts.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static PhiliaContacts.Domains.EmailAddress;
using static PhiliaContacts.Domains.PhoneNumber;

namespace PhiliaContacts.App.Base.Controls
{
    public sealed partial class ContactDetailControl : UserControl
    {
        public List<Contact.AddressTypes> AddressTypes { get; set; } = Enum.GetValues(typeof(Contact.AddressTypes)).Cast<Contact.AddressTypes>().ToList();

        public Contact MasterContact
        {
            get { return GetValue(MasterContactProperty) as Contact; }
            set { SetValue(MasterContactProperty, value); }
        }

        public static readonly DependencyProperty MasterContactProperty = DependencyProperty.Register(
            nameof(MasterContact), 
            typeof(Contact), 
            typeof(ContactDetailControl), 
            new PropertyMetadata(null, OnMasterContactPropertyChanged));

        public ContactDetailControl()
        {
            this.InitializeComponent();
        }

        private static void OnMasterContactPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ContactDetailControl;
            control.ForegroundElement.ChangeView(0, 0, 1);
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            MasterContact.IsFavorite = !MasterContact.IsFavorite;
        }

        private void BirthdayClearButton_Click(object sender, RoutedEventArgs e)
        {
            MasterContact.Birthday = null;
        }

        #region Photo
        private void Button_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            AddPhotoButton.Opacity = 1.0D;
            AddPhotoButtonBackground.Opacity = 0.8D;
        }

        private void Button_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            AddPhotoButton.Opacity = 0.0D;
            AddPhotoButtonBackground.Opacity = 0.0D;
        }

        private async void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] image = await PickPhotoAsync();

            if (image != null)
            {
                MasterContact.Photo = image;
            }
        }

        private async Task<byte[]> PickPhotoAsync()
        {
            FileOpenPicker filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".bmp");
            filePicker.FileTypeFilter.Add(".png");

            StorageFile file = await filePicker.PickSingleFileAsync();

            if (file != null)
            {
                return await Images.StorageFileToBytesAsync(file);
            }

            return null;
        }
        #endregion

        #region Email
        public EmailAddress SelectedEmail;

        public List<EmailType> EmailTypes { get; set; } = Enum.GetValues(typeof(EmailType)).Cast<EmailType>().ToList();

        private void AddEmailButton_Click(object sender, RoutedEventArgs e)
        {
            EmailAddress newEmailAddress = new EmailAddress() { Type = EmailType.None };

            MasterContact.EmailAddresses.Add(newEmailAddress);

            SelectedEmail = newEmailAddress;
        }

        private void DeleteEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmail != null)
            {
                MasterContact.EmailAddresses.Remove(SelectedEmail);
                SelectedEmail = null;
            }
        }
        #endregion

        #region Phone
        public PhoneNumber SelectedPhoneNumber;

        public List<PhoneNumberType> PhoneNumberTypes { get; set; } = Enum.GetValues(typeof(PhoneNumberType)).Cast<PhoneNumberType>().ToList();

        private void AddPhoneButton_Click(object sender, RoutedEventArgs e)
        {
            PhoneNumber newPhoneNumber = new PhoneNumber() { Type = PhoneNumberType.None };

            MasterContact.PhoneNumbers.Add(newPhoneNumber);

            SelectedPhoneNumber = newPhoneNumber;
        }

        private void DeletePhoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPhoneNumber != null)
            {
                MasterContact.PhoneNumbers.Remove(SelectedPhoneNumber);
                SelectedPhoneNumber = null;
            }
        }
        #endregion

        #region Country
        private void CountrySearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<string> suitableItems = new List<string>();
            string[] splitText = sender.Text.ToLower().Split(" ");

            foreach (string country in Manager.Countries)
            {
                bool found = splitText.All((key) =>
                {
                    return country.ToLower().Contains(key);
                });

                if (found)
                {
                    suitableItems.Add(country);
                }
            }

            if (suitableItems.Count == 0)
            {
                suitableItems.Add("No results found");
            }

            sender.ItemsSource = suitableItems;

        }

        private void CountrySearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            CountrySearchBox.Text = args.SelectedItem.ToString();
        }
        #endregion
    }
}
