using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Contacts;

public interface IContactService
{
    Task<IReadOnlyList<Contact>> ListAsync(CancellationToken cancellationToken = default);
    Task<Contact?> GetAsync(ContactId id, CancellationToken cancellationToken = default);
    Task SaveAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(ContactId id, CancellationToken cancellationToken = default);
}

public interface IContactStore
{
    Task<IReadOnlyList<Contact>> ListAsync(CancellationToken cancellationToken = default);
    Task<Contact?> GetAsync(ContactId id, CancellationToken cancellationToken = default);
    Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(ContactId id, CancellationToken cancellationToken = default);
}

// Parsing is an external-format boundary; discovery, backup and import orchestration follow in Phase 2.
public interface ILegacyContactReader
{
    IReadOnlyList<Contact> Read(string json);
}

public enum LegacyImportOutcome
{
    Imported,
    AlreadyImported,
    Empty,
    ChangedSource,
    NeedsSelection
}

public sealed record LegacyImportResult(string SourcePath, LegacyImportOutcome Outcome, int ContactCount, string? BackupPath = null);

public interface ILegacyContactImportStore
{
    Task<bool> HasContactsAsync(CancellationToken cancellationToken = default);
    Task<LegacyImportOutcome?> GetStatusAsync(string sourcePath, string sha256, CancellationToken cancellationToken = default);
    Task<LegacyImportOutcome> ImportAsync(string sourcePath, string sha256, IReadOnlyList<Contact> contacts, CancellationToken cancellationToken = default);
}

public interface ILegacyContactImportService
{
    Task<IReadOnlyList<string>> DiscoverAsync(CancellationToken cancellationToken = default);
    Task<LegacyImportResult> ImportAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LegacyImportResult>> ImportDiscoveredAsync(CancellationToken cancellationToken = default);
}

public sealed record LegacyContactFile(string SourcePath, string Json, string Sha256, byte[] Bytes);

public interface ILegacyContactFiles
{
    Task<IReadOnlyList<string>> DiscoverAsync(CancellationToken cancellationToken = default);
    Task<LegacyContactFile> ReadAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<string> BackupAsync(LegacyContactFile file, CancellationToken cancellationToken = default);
}

public interface IContactExportService
{
    Task ExportAsync(string destinationPath, CancellationToken cancellationToken = default);
}
