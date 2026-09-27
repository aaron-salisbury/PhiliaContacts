using Dapper;
using Microsoft.Data.Sqlite;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Data.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Contacts;

internal sealed class SqliteLegacyContactImportStore : ILegacyContactImportStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteLegacyContactImportStore(IPhiliaContactsDatabase database)
    {
        _database = database;
    }

    public async Task<bool> HasContactsAsync(CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM Contact)", cancellationToken: cancellationToken)) != 0;
    }

    public async Task<LegacyImportOutcome?> GetStatusAsync(string sourcePath, string sha256, CancellationToken cancellationToken = default)
    {
        sourcePath = NormalizePath(sourcePath);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        string? recordedHash = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT Sha256 FROM LegacyContactImport WHERE SourcePath = @sourcePath", new { sourcePath }, cancellationToken: cancellationToken));
        return recordedHash is null ? null : HashStatus(recordedHash, sha256);
    }

    public async Task<LegacyImportOutcome> ImportAsync(string sourcePath, string sha256, IReadOnlyList<Contact> contacts, CancellationToken cancellationToken = default)
    {
        sourcePath = NormalizePath(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        ArgumentNullException.ThrowIfNull(contacts);
        if (contacts.Count == 0)
        {
            return LegacyImportOutcome.Empty;
        }

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction();
        string? recordedHash = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT Sha256 FROM LegacyContactImport WHERE SourcePath = @sourcePath", new { sourcePath }, transaction, cancellationToken: cancellationToken));
        if (recordedHash is not null)
        {
            return HashStatus(recordedHash, sha256);
        }

        foreach (Contact contact in contacts)
        {
            await SqliteContactStore.WriteAsync(connection, transaction, contact, cancellationToken);
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO LegacyContactImport (SourcePath, Sha256, ContactCount, ImportedAtUtc) VALUES (@sourcePath, @sha256, @count, CURRENT_TIMESTAMP)",
            new { sourcePath, sha256, count = contacts.Count }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return LegacyImportOutcome.Imported;
    }

    private static LegacyImportOutcome HashStatus(string recordedHash, string sha256)
    {
        return string.Equals(recordedHash, sha256, StringComparison.OrdinalIgnoreCase)
            ? LegacyImportOutcome.AlreadyImported : LegacyImportOutcome.ChangedSource;
    }

    private static string NormalizePath(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        string fullPath = Path.GetFullPath(sourcePath);
        return OperatingSystem.IsWindows() ? fullPath.ToUpperInvariant() : fullPath;
    }
}
