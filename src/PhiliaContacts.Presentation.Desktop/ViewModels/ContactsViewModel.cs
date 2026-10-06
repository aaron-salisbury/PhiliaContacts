using Avalonia.Animation.Easings;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Presentation.Desktop.Base.Controls.RibbonControls;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Presentation.Desktop.ViewModels;

public partial class ContactsViewModel : BaseViewModel, IRibbonProvider
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

    public ObservableCollection<Contact> Contacts { get; } = [];
    public ObservableCollection<Contact> VisibleContacts { get; } = [];

    public Type RibbonControlType => typeof(ContactsRibbonControl);

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

        SelectedContact = Contacts.FirstOrDefault();

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
            //TODO: Set SelectedContact to the next contact in the list, or previous if the was the last one. If none, set to null.
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

    public async Task ImportVCardAsync(Stream stream)
    {
        // 1. Do I/O asynchronously (ReadToEndAsync, WriteAsync).
        // 2. Do CPU - heavy parsing / transforms off UI thread(Task.Run), but keep it pure(no UI collection / property writes there).
        // 3. Apply results on UI thread(update ObservableCollection, Status, etc.).
        // For Avalonia specifically:
        //    •	ObservableCollection and bound property updates should happen on UI thread.
        //    •	If command continuation is guaranteed on UI context, direct updates are fine.
        //    •	If context is uncertain, use Dispatcher.UIThread.InvokeAsync(...) for the UI-update block.

        try
        {
            IsBusy = true;

            string vCardText;
            using (StreamReader reader = new(stream, new UTF8Encoding(false, true), true, 1024, true))
            {
                vCardText = await reader.ReadToEndAsync();
            }

            IReadOnlyList<Contact> incoming = await Task.Run(() => _vCards.Read(vCardText));

            int count = 0;
            foreach (Contact contact in incoming)
            {
                await _contacts.SaveAsync(contact);
                count++;
            }

            await RefreshAsync();
            LongRunningProcessSuccessful = true;
            Status = $"Imported {count} vCard contacts.";
        }
        catch (Exception error)
        {
            LongRunningProcessSuccessful = false;
            Status = "Could not import vCard: " + error.Message;
        }
        finally
        {
            IsBusy = false;
            LongRunningProcessSuccessful = null;
        }
    }

    public async Task ExportVCardAsync(Stream stream)
    {
        try
        {
            IsBusy = true;

            IReadOnlyList<Contact> contacts = await _contacts.ListAsync();
            string text = await Task.Run(() => _vCards.Write(contacts));
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            await stream.WriteAsync(bytes);

            LongRunningProcessSuccessful = true;
            Status = "vCard export saved.";
        }
        catch (Exception error)
        {
            LongRunningProcessSuccessful = false;
            Status = "vCard export failed: " + error.Message;
        }
        finally
        {
            IsBusy = false;
            LongRunningProcessSuccessful = null;
        }
    }
}
