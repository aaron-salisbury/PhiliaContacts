using PhiliaContacts.App.Base.Helpers;
using PhiliaContacts.App.ViewModels;
using PhiliaContacts.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace PhiliaContacts.App.Views
{
    public sealed partial class ContactsPage : Page
    {
        public ContactsViewModel ViewModel { get; } = new ContactsViewModel();

        public ContactsPage()
        {
            this.InitializeComponent();
            DataContext = ViewModel;

            ViewModel.WorkflowSuccessAction = this.AppBar.AnimateSucess;
            ViewModel.WorkflowFailureAction = this.AppBar.AnimateFailure;
        }

        #region Contact Search Box
        private void ContactSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                RunSearch(sender.Text);
            }
        }

        private void ContactSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null && args.ChosenSuggestion is Contact)
            {
                Contact chosen = args.ChosenSuggestion as Contact;

                if (ViewModel.Manager.Contacts.Contains(chosen))
                {
                    ViewModel.Selected = chosen;

                    // Clear search box.
                    sender.Text = string.Empty;
                    ContactSearchBox.ItemsSource = null;

                    // Scroll to selected item.
                    List<ListView> listViews = new List<ListView>();
                    ElementFinder.FindChildren(listViews, MasterDetailsViewControl);
                    ListView masterList = listViews.FirstOrDefault();
                    if (masterList != null)
                    {
                        masterList.ScrollIntoView(chosen);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(args.QueryText))
            {
                RunSearch(args.QueryText);
            }
        }

        private void ContactSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Prevent "No results" from being the value in the search box, since it doesn't serve anything.
            if (args.SelectedItem == null || !(args.SelectedItem is Contact))
            {
                sender.Text = string.Empty;
            }
            else
            {
                sender.Text = (args.SelectedItem as Contact).ToString();
            }
        }

        private void RunSearch(string queryText)
        {
            HashSet<Contact> suggestions = new HashSet<Contact>();

            foreach (string queryToken in queryText.Split(" "))
            {
                suggestions.UnionWith(ViewModel.Manager.Contacts
                    .Where(c => !string.IsNullOrEmpty(c.Nickname) && c.Nickname.StartsWith(queryToken, StringComparison.CurrentCultureIgnoreCase)));

                suggestions.UnionWith(ViewModel.Manager.Contacts
                    .Where(c => !string.IsNullOrEmpty(c.FamilyName) && c.FamilyName.StartsWith(queryToken, StringComparison.CurrentCultureIgnoreCase)));

                suggestions.UnionWith(ViewModel.Manager.Contacts
                    .Where(c => !string.IsNullOrEmpty(c.GivenName) && c.GivenName.StartsWith(queryToken, StringComparison.CurrentCultureIgnoreCase)));
            }

            if (suggestions.Count > 0)
            {
                ContactSearchBox.ItemsSource = suggestions.OrderBy(s => s.FamilyName).ThenBy(s => s.GivenName);
            }
            else
            {
                ContactSearchBox.ItemsSource = new string[] { "No results" };
            }
        }
        #endregion
    }
}
