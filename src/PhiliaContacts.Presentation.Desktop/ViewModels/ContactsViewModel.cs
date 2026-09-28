using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhiliaContacts.Business.Modules.Contacts;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhiliaContacts.Presentation.Desktop.ViewModels;

public partial class ImportCandidate : ObservableObject
{
    public Contact Contact { get; }
    public string Name => Contact.DisplayName;
    public string Hint { get; }

    [ObservableProperty] private bool _isSelected;

    public ImportCandidate(Contact contact, bool possibleDuplicate)
    {
        Contact = contact;
        Hint = possibleDuplicate ? "Possible duplicate — review before adding" : "New contact";
        IsSelected = !possibleDuplicate;
    }
}

public partial class ContactsViewModel : BaseViewModel
{
    private readonly IContactService _contacts;
    private readonly ILegacyContactImportService _legacy;
    private readonly IContactExportService _export;
    private readonly IVCardContactService _vCards;
    private bool _loaded;

    [ObservableProperty] private Contact? _selectedContact;
    [ObservableProperty] private ContactDetailViewModel _detail = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _status = "Loading contacts…";
    [ObservableProperty] private bool _hasContacts;
    [ObservableProperty] private bool _hasPreview;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _previewTitle = string.Empty;

    public ObservableCollection<Contact> Contacts { get; } = [];
    public ObservableCollection<Contact> VisibleContacts { get; } = [];
    public ObservableCollection<ImportCandidate> Candidates { get; } = [];

    public ContactsViewModel(IContactService contacts, ILegacyContactImportService legacy, IContactExportService export, IVCardContactService vCards)
    {
        _contacts = contacts;
        _legacy = legacy;
        _export = export;
        _vCards = vCards;
    }

    public override async Task InitializeAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        await RefreshAsync();

        try
        {
            IReadOnlyList<string> sources = await _legacy.DiscoverAsync();
            if (sources.Count > 1)
            {
                Status += " Multiple legacy files were found; choose one under Import older data.";
            }
        }
        catch (Exception error)
        {
            Status = "Legacy data search failed: " + error.Message;
        }
    }

    partial void OnSelectedContactChanged(Contact? value)
    {
        Detail.Dispose();
        Detail = new ContactDetailViewModel(value);
    }

    partial void OnSearchTextChanged(string value) => Filter();

    private void Filter()
    {
        string query = SearchText.Trim();
        VisibleContacts.Clear();
        foreach (Contact contact in Contacts)
        {
            if (query.Length == 0 || new[]{ contact.GivenName, contact.FamilyName, contact.Nickname, contact.Organization,
                contact.PhoneticGivenName, contact.PhoneticFamilyName }.Any(value => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
            {
                VisibleContacts.Add(contact);
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ContactId? selectedId = SelectedContact?.Id;

        try
        {
            IsBusy = true;
            IReadOnlyList<Contact> items = await _contacts.ListAsync();
            Contacts.Clear();

            foreach (Contact contact in items)
            {
                Contacts.Add(contact);
            }

            Filter();
            HasContacts = Contacts.Count > 0;
            SelectedContact = selectedId is null ? null : Contacts.FirstOrDefault(contact => contact.Id == selectedId);
            Status = Contacts.Count == 0 ? "No contacts yet. Add one or import older data." : $"{Contacts.Count} contacts";
        }
        catch (Exception error)
        {
            Status = "Could not load contacts: " + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewContact()
    {
        SelectedContact = null;
        Detail.Dispose();
        Detail = new ContactDetailViewModel();
        Status = "New contact. Save when finished.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            Contact contact = Detail.ToContact();
            await _contacts.SaveAsync(contact);
            await RefreshAsync();
            SelectedContact = Contacts.First(item => item.Id == contact.Id);
            Status = "Contact saved.";
        }
        catch (Exception error)
        {
            Status = "Could not save: " + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedContact is null)
        {
            Status = "Select a saved contact to delete."; return;
        }

        try
        {
            IsBusy = true;
            await _contacts.DeleteAsync(SelectedContact.Id);
            SelectedContact = null;
            await RefreshAsync();
            Status = "Contact deleted.";
        }
        catch (Exception error)
        {
            Status = "Could not delete: " + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddPhone()
    {
        Detail.PhoneNumbers.Add(new EditableValue());
    }

    [RelayCommand] private void RemovePhone()
    {
        if (Detail.SelectedPhone is { } item)
        {
            Detail.PhoneNumbers.Remove(item);
        }
    }

    [RelayCommand]
    private void AddEmail()
    {
        Detail.EmailAddresses.Add(new EditableValue());
    }

    [RelayCommand] private void RemoveEmail()
    {
        if (Detail.SelectedEmail is { } item)
        {
            Detail.EmailAddresses.Remove(item);
        }
    }

    [RelayCommand]
    private void AddAddress()
    {
        Detail.Addresses.Add(new EditableAddress());
    }

    [RelayCommand] private void RemoveAddress()
    {
        if (Detail.SelectedAddress is { } item)
        {
            Detail.Addresses.Remove(item);
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        Detail.SetPhoto(null);
    }

    public async Task SetPhotoAsync(Stream stream)
    {
        try
        {
            using MemoryStream buffer = new();
            await stream.CopyToAsync(buffer);
            Detail.SetPhoto(buffer.ToArray());
            Status = "Photo selected. Save the contact to keep it.";
        }
        catch (Exception error)
        {
            Status = "Could not use photo: " + error.Message;
        }
    }

    public async Task ImportOlderAsync(string path)
    {
        try
        {
            IsBusy = true;
            LegacyImportResult result = await _legacy.ImportAsync(path);
            await RefreshAsync();
            Status = $"Older data: {result.Outcome} ({result.ContactCount} contacts)." +
                (result.BackupPath is null ? string.Empty : $" Backup: {result.BackupPath}");
        }
        catch (Exception error)
        {
            Status = "Import failed; the source was retained: " + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportJsonAsync(string path)
    {
        try
        {
            await _export.ExportAsync(path); Status = "JSON export saved.";
        }
        catch (Exception error)
        {
            Status = "Export failed: " + error.Message;
        }
    }

    public async Task PreviewVCardAsync(Stream stream)
    {
        try
        {
            using StreamReader reader = new(stream, new UTF8Encoding(false, true), true, 1024, true);
            IReadOnlyList<Contact> incoming = _vCards.Read(await reader.ReadToEndAsync());
            Candidates.Clear();
            foreach (Contact contact in incoming)
            {
                bool match = Contacts.Any(existing =>
                    string.Equals(existing.DisplayName, contact.DisplayName, StringComparison.OrdinalIgnoreCase) &&
                    existing.EmailAddresses.Any(email => contact.EmailAddresses.Any(candidate =>
                        string.Equals(email.Value, candidate.Value, StringComparison.OrdinalIgnoreCase))));
                Candidates.Add(new(contact, match));
            }
            HasPreview = Candidates.Count > 0;
            PreviewTitle = $"Review {Candidates.Count} vCard contacts (possible duplicates are unchecked)";
            Status = HasPreview ? "Select the contacts to add, then confirm import." : "No vCards in the file.";
        }
        catch (Exception error)
        {
            HasPreview = false;
            Status = "Could not read vCard: " + error.Message;
        }
    }

    [RelayCommand]
    private async Task ConfirmVCardImportAsync()
    {
        try
        {
            IsBusy = true;
            int count = 0;
            foreach (ImportCandidate item in Candidates.Where(item => item.IsSelected))
            {
                await _contacts.SaveAsync(item.Contact);
                count++;
            }
            Candidates.Clear();
            HasPreview = false;
            await RefreshAsync();
            Status = $"Imported {count} vCard contacts.";
        }
        catch (Exception error)
        {
            Status = "Import stopped: " + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelVCardImport()
    {
        Candidates.Clear();
        HasPreview = false;
        Status = "Import cancelled.";
    }

    public async Task ExportVCardAsync(Stream stream)
    {
        try
        {
            string text = _vCards.Write(await _contacts.ListAsync());
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            await stream.WriteAsync(bytes);
            Status = "vCard export saved.";
        }
        catch (Exception error)
        {
            Status = "vCard export failed: " + error.Message;
        }
    }

    public async Task ExportJsonAsync(Stream stream)
    {
        try
        {
            await _export.ExportAsync(stream);
            Status = "JSON export saved.";
        }
        catch (Exception error)
        {
            Status = "Export failed: " + error.Message;
        }
    }
}
