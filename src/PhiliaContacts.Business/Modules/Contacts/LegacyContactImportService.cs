using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Contacts;

internal sealed class LegacyContactImportService : ILegacyContactImportService
{
    private readonly ILegacyContactFiles _files;
    private readonly ILegacyContactReader _reader;
    private readonly ILegacyContactImportStore _store;

    public LegacyContactImportService(ILegacyContactFiles files, ILegacyContactReader reader, ILegacyContactImportStore store)
    {
        _files = files;
        _reader = reader;
        _store = store;
    }

    public Task<IReadOnlyList<string>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        return _files.DiscoverAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LegacyImportResult>> ImportDiscoveredAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> paths = await DiscoverAsync(cancellationToken);
        List<(LegacyContactFile File, IReadOnlyList<Contact> Contacts)> populated = [];
        List<LegacyImportResult> results = [];

        foreach (string path in paths)
        {
            LegacyContactFile file = await _files.ReadAsync(path, cancellationToken);
            IReadOnlyList<Contact> contacts = _reader.Read(file.Json);
            if (contacts.Count == 0)
            {
                results.Add(new(path, LegacyImportOutcome.Empty, 0));
            }
            else
            {
                populated.Add((file, contacts));
            }
        }

        if (populated.Count > 1)
        {
            results.AddRange(populated.Select(item => new LegacyImportResult(item.File.SourcePath, LegacyImportOutcome.NeedsSelection, item.Contacts.Count)));
            return results;
        }

        if (populated.Count == 1)
        {
            (LegacyContactFile file, IReadOnlyList<Contact> contacts) = populated[0];
            LegacyImportOutcome? status = await _store.GetStatusAsync(file.SourcePath, file.Sha256, cancellationToken);
            if (status is null && await _store.HasContactsAsync(cancellationToken))
            {
                results.Add(new(file.SourcePath, LegacyImportOutcome.NeedsSelection, contacts.Count));
            }
            else
            {
                results.Add(await ImportFileAsync(file, contacts, cancellationToken));
            }
        }

        return results;
    }

    public async Task<LegacyImportResult> ImportAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        LegacyContactFile file = await _files.ReadAsync(sourcePath, cancellationToken);
        return await ImportFileAsync(file, _reader.Read(file.Json), cancellationToken);
    }

    private async Task<LegacyImportResult> ImportFileAsync(LegacyContactFile file, IReadOnlyList<Contact> contacts, CancellationToken cancellationToken)
    {
        if (contacts.Count == 0)
        {
            return new(file.SourcePath, LegacyImportOutcome.Empty, 0);
        }

        foreach (Contact contact in contacts)
        {
            ContactValidation.Validate(contact);
        }

        LegacyImportOutcome? status = await _store.GetStatusAsync(file.SourcePath, file.Sha256, cancellationToken);
        if (status is not null)
        {
            return new(file.SourcePath, status.Value, contacts.Count);
        }

        string backupPath = await _files.BackupAsync(file, cancellationToken);
        LegacyImportOutcome outcome = await _store.ImportAsync(file.SourcePath, file.Sha256, contacts, cancellationToken);
        return new(file.SourcePath, outcome, contacts.Count, backupPath);
    }
}
