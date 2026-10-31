using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Presentation.Desktop.Base.Controls.RibbonControls;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhiliaContacts.Presentation.Desktop.ViewModels;

public partial class ContactsViewModel : BaseViewModel, IRibbonProvider
{
    private readonly IContactService _contacts;
    private readonly ILegacyContactImportService _legacy;
    private readonly IContactExportService _export;
    private readonly IVCardContactService _vCards;
    private bool _loaded;
    private bool _preserveDetail;
    private bool _syncingListSelection;

    /// <summary>
    /// The contact whose details are shown. Set programmatically; user selection goes through <see cref="ListSelection"/>.
    /// </summary>
    [ObservableProperty] private Contact? _selectedContact;

    /// <summary>
    /// The list's selected item. A user selection is only applied to <see cref="SelectedContact"/> once any unsaved
    /// changes in <see cref="Detail"/> may be discarded.
    /// </summary>
    [ObservableProperty] private Contact? _listSelection;

    [ObservableProperty] private ContactDetailViewModel _detail;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _status = "Loading contacts…";
    [ObservableProperty] private bool _hasContacts;

    public ObservableCollection<Contact> Contacts { get; } = [];
    public ObservableCollection<Contact> VisibleContacts { get; } = [];

    /// <summary>
    /// Supplied by the view to ask the user whether unsaved contact changes may be discarded.
    /// </summary>
    public Func<Task<bool>>? ConfirmDiscardChangesAsync { get; set; }

    public Type RibbonControlType => typeof(ContactsRibbonControl);

    public ContactsViewModel(IContactService contacts, ILegacyContactImportService legacy, IContactExportService export, IVCardContactService vCards)
    {
        _contacts = contacts;
        _legacy = legacy;
        _export = export;
        _vCards = vCards;
        _detail = new ContactDetailViewModel();
        _detail.MessageRaised += Detail_MessageRaised;
    }

    public override async Task InitializeAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        await RefreshAsync();

        if (SelectedContact is null && !Detail.IsDirty)
        {
            SelectedContact = VisibleContacts.FirstOrDefault();
        }

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

    partial void OnListSelectionChanged(Contact? value)
    {
        // Null arrives when the list's items are cleared by filtering or reloading; keep showing the current detail.
        if (_syncingListSelection || value is null || ReferenceEquals(value, SelectedContact))
        {
            return;
        }

        _ = SelectFromListAsync(value);
    }

    private async Task SelectFromListAsync(Contact value)
    {
        if (await CanDiscardChangesAsync())
        {
            SelectedContact = value;
        }
        else
        {
            SyncListSelection();
        }
    }

    partial void OnSelectedContactChanged(Contact? value)
    {
        SyncListSelection();

        if (_preserveDetail && value is not null && value.Id == Detail.Id)
        {
            return;
        }

        ReplaceDetail(new ContactDetailViewModel(value));
    }

    private void ReplaceDetail(ContactDetailViewModel next)
    {
        ContactDetailViewModel previous = Detail;
        previous.MessageRaised -= Detail_MessageRaised;
        next.MessageRaised += Detail_MessageRaised;

        // Switch the binding to the new editor before disposing the old one, so the view never shows a disposed model.
        Detail = next;
        previous.Dispose();
    }

    private void Detail_MessageRaised(object? sender, string message)
    {
        Status = message;
    }

    private void SyncListSelection()
    {
        _syncingListSelection = true;
        try
        {
            ListSelection = SelectedContact is not null && VisibleContacts.Contains(SelectedContact) ? SelectedContact : null;
        }
        finally
        {
            _syncingListSelection = false;
        }
    }

    public async Task<bool> CanDiscardChangesAsync()
    {
        return !Detail.IsDirty || ConfirmDiscardChangesAsync is null || await ConfirmDiscardChangesAsync();
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

        SyncListSelection();
    }

    /// <summary>
    /// Reloads contacts and selects <paramref name="selectId"/>. Unsaved edits to that same contact are kept unless
    /// <paramref name="rebuildDetail"/> is set. Does not change <see cref="BaseViewModel.IsBusy"/>; callers own it.
    /// </summary>
    private async Task ReloadAsync(ContactId? selectId, bool rebuildDetail = false)
    {
        IReadOnlyList<Contact> items = await _contacts.ListAsync();
        Contacts.Clear();

        foreach (Contact contact in items)
        {
            Contacts.Add(contact);
        }

        Filter();
        HasContacts = Contacts.Count > 0;

        _preserveDetail = !rebuildDetail;
        try
        {
            SelectedContact = selectId is null ? null : Contacts.FirstOrDefault(contact => contact.Id == selectId);
        }
        finally
        {
            _preserveDetail = false;
        }

        // A deleted or missing selection leaves an empty editor rather than a stale one.
        if (SelectedContact is null && selectId is not null && Detail.Id == selectId)
        {
            ReplaceDetail(new ContactDetailViewModel());
        }
    }

    private string CountStatus()
    {
        return Contacts.Count == 0 ? "No contacts yet. Add one or import older data." : $"{Contacts.Count} contacts";
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            await ReloadAsync(SelectedContact?.Id);
            Status = CountStatus();
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
    private async Task NewContactAsync()
    {
        if (!await CanDiscardChangesAsync())
        {
            return;
        }

        if (SelectedContact is null)
        {
            ReplaceDetail(new ContactDetailViewModel());
        }
        else
        {
            SelectedContact = null;
        }

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
            Detail.AcceptChanges();
            await ReloadAsync(contact.Id);
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

            Contact deleted = SelectedContact;
            int index = VisibleContacts.IndexOf(deleted);
            Contact? neighbor = index < 0 ? null : VisibleContacts.ElementAtOrDefault(index + 1) ?? VisibleContacts.ElementAtOrDefault(index - 1);

            await _contacts.DeleteAsync(deleted.Id);
            await ReloadAsync(neighbor?.Id, rebuildDetail: true);

            if (SelectedContact is null)
            {
                ReplaceDetail(new ContactDetailViewModel());
            }

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
            await ReloadAsync(SelectedContact?.Id);
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

        bool committed = false;
        string importedSummary = string.Empty;

        try
        {
            IsBusy = true;

            string vCardText;
            using (StreamReader reader = new(stream, new UTF8Encoding(false, true), true, 1024, true))
            {
                vCardText = await reader.ReadToEndAsync();
            }

            IReadOnlyList<Contact> parsed = await Task.Run(() => _vCards.Read(vCardText));

            // Cards carrying a UID that is already stored update that contact instead of adding a duplicate. The update is a
            // merge, so fields the card leaves out (or that the importer skipped, such as a remote photo) are not erased.
            IReadOnlyList<Contact> stored = await _contacts.ListAsync();
            HashSet<ContactId> existing = [.. stored.Select(contact => contact.Id)];
            IReadOnlyList<Contact> incoming = ContactImportMerge.MergeIntoStored(stored, parsed);
            HashSet<ContactId> incomingIds = [.. incoming.Select(contact => contact.Id)];
            int updated = incomingIds.Count(existing.Contains);

            // All-or-nothing: a failure part-way through leaves the stored contacts unchanged.
            await _contacts.SaveManyAsync(incoming);
            committed = true;
            importedSummary = $"Imported {incomingIds.Count} vCard contacts ({incomingIds.Count - updated} new, {updated} updated).";

            // The caller confirms discarding unsaved edits before importing (see CanDiscardChangesAsync), so the editor
            // always reloads; a stale snapshot saved later would otherwise overwrite what was just imported.
            await ReloadAsync(SelectedContact?.Id, rebuildDetail: true);
            if (SelectedContact is null)
            {
                ReplaceDetail(new ContactDetailViewModel());
            }

            LongRunningProcessSuccessful = true;
            Status = importedSummary;
        }
        catch (Exception error) when (committed)
        {
            // The import itself succeeded; only refreshing the list failed.
            LongRunningProcessSuccessful = true;
            Status = $"{importedSummary} The list could not be refreshed: {error.Message}";
        }
        catch (Exception error)
        {
            LongRunningProcessSuccessful = false;
            Status = "Could not import vCard; no contacts were changed: " + error.Message;
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
