using PhiliaContacts.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static PhiliaContacts.Domains.EmailAddress;
using static PhiliaContacts.Domains.PhoneNumber;

namespace PhiliaContacts.App.Base.Controls
{
    public sealed partial class ContactDetailControl : UserControl
    {
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

            //EmailGrid.Columns.Where(c => string.Equals(c.Name, "EmailColumn")).FirstOrDefault().Width = 307;
            //EmailGrid.Columns.Where(c => string.Equals(c.Name, "EmailTypeColumn")).FirstOrDefault().Width = 153;
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
    }
}
