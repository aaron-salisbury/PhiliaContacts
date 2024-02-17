using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhiliaContacts.Business;
using PhiliaContacts.Business.Models;
using PhiliaContacts.Presentation.Base;
using PhiliaContacts.Presentation.Base.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhiliaContacts.Presentation.ViewModels
{
    public partial class ContactsViewModel : BaseViewModel
    {
        public RelayCommand NewContactCommand { get; }
        public RelayCommand DeleteContactCommand { get; }
        public RelayCommand SaveCommand { get; }

        [ObservableProperty]
        private FullyObservableCollection<Contact> _contacts;

        [ObservableProperty]
        private IEnumerable<string> _names;

        [ObservableProperty]
        private Contact? _selectedContact;

        [ObservableProperty]
        private bool _isDirty;

        private readonly IAgnosticDispatcher _dispatcher;

        public ContactsViewModel(IAgnosticDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _contacts = GetOrderedObservableContacts(Task.Run(() => Manager.GetContactsAsync()).Result);
            _names = GetContactNames();
            _selectedContact = _contacts.FirstOrDefault();

            NewContactCommand = new RelayCommand(() => AddNewContact());
            DeleteContactCommand = new RelayCommand(() => DeleteContact());
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);
        }

        public async Task ImportAsync(Stream virtualContactFileStream)
        {
            if (!IsBusy)
            {
                try
                {
                    using StreamReader streamReader = new(virtualContactFileStream);
                    string fileContent = await streamReader.ReadToEndAsync();

                    List<Contact>? newContacts = (await InitiateLongRunningProcessAsync(() => Importer.Import(fileContent), _dispatcher))?.ToList();

                    if (newContacts != null)
                    {
                        newContacts.AddRange(Contacts);
                        Contacts = GetOrderedObservableContacts(newContacts);
                        IsDirty = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to import selected VCF file: {ex.Message}", "ERROR");
                }
            }
        }

        public async Task ExportAsync(Stream storageLocationStream)
        {
            if (!IsBusy)
            {
                try
                {
                    string? virtualContactFileText = await InitiateLongRunningProcessAsync(() => Exporter.Export(Contacts), _dispatcher);

                    if (virtualContactFileText != null)
                    {
                        using var streamWriter = new StreamWriter(storageLocationStream);
                        await streamWriter.WriteLineAsync(virtualContactFileText);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to export VCF to selected storage location: {ex.Message}", "ERROR");
                }
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                if ((await InitiateLongRunningProcessAsync(async () => await Manager.SaveAsync(Contacts), _dispatcher)).Result)
                {
                    Contacts = GetOrderedObservableContacts(Contacts);

                    IsDirty = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save contacts: {ex.Message}", "ERROR");
            }
        }

        private void AddNewContact()
        {
            Contact newContact = new() { Photo = Manager.DefaultUserImage };

            Contacts.Add(newContact);

            SelectedContact = newContact;
        }

        private void DeleteContact()
        {
            if (SelectedContact != null)
            {
                int selectedIndex = Contacts.IndexOf(SelectedContact);
                int newIndex = selectedIndex == (Contacts.Count - 1) ? selectedIndex - 1 : selectedIndex;

                Contacts.Remove(SelectedContact);

                SelectedContact = newIndex >= 0 ? Contacts[newIndex] : null;
            }
        }

        private FullyObservableCollection<Contact> GetOrderedObservableContacts(IEnumerable<Contact> contacts)
        {
            FullyObservableCollection<Contact> orderedObservableContacts = new(contacts
                .OrderBy(c => !c.IsFavorite)
                .ThenBy(c => c.DisplayName)
                .ThenBy(c => c.GivenName));

            orderedObservableContacts.ItemPropertyChanged += Contacts_OnItemPropertyChanged;
            orderedObservableContacts.CollectionChanged += Contacts_OnCollectionChanged;

            return orderedObservableContacts;
        }

        private void Contacts_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
        }

        private void Contacts_OnItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            IsDirty = true;

            switch (e.PropertyName)
            {
                case nameof(Contact.GivenName):
                case nameof(Contact.FamilyName):
                    Names = GetContactNames();
                    break;
            }
        }

        private IEnumerable<string> GetContactNames()
        {
            return Contacts.Select(c => c.FormattedName);
        }
    }
}
