using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Integrations.LegacyContacts;

internal sealed class LegacyContactFiles : ILegacyContactFiles
{
    private const int MAX_SOURCE_BYTES = 64 * 1024 * 1024;
    private static readonly string[] FileNames = ["Contact.json", "PhiliaContacts.json"];
    private readonly string _applicationDataDirectory;
    private readonly IReadOnlyList<string> _searchDirectories;

    internal LegacyContactFiles(string applicationDataDirectory, IReadOnlyList<string>? searchDirectories = null)
    {
        _applicationDataDirectory = applicationDataDirectory;
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _searchDirectories = searchDirectories ?? (OperatingSystem.IsWindows()
            ? [Path.Combine(localAppData, "Packages", "60826AaronSalisbury.PhiliaContacts_gc14fakmyh3dc", "LocalState"), applicationDataDirectory]
            : []);
    }

    public Task<IReadOnlyList<string>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> paths = [.. _searchDirectories
            .Where(Directory.Exists)
            .SelectMany(directory => FileNames.Select(fileName => Path.Combine(directory, fileName)))
            .Where(File.Exists)
            .Select(Path.GetFullPath)
            .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)];
        return Task.FromResult(paths);
    }

    public async Task<LegacyContactFile> ReadAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        string path = Path.GetFullPath(sourcePath);
        if (!FileNames.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Select a Contact.json or PhiliaContacts.json file.", nameof(sourcePath));
        }

        await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        if (stream.Length > MAX_SOURCE_BYTES)
        {
            throw new InvalidDataException("The legacy file exceeds the 64 MiB import limit.");
        }

        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer, cancellationToken);
        byte[] bytes = buffer.ToArray();
        if (bytes.Length > MAX_SOURCE_BYTES)
        {
            throw new InvalidDataException("The legacy file exceeds the 64 MiB import limit.");
        }

        string json = new UTF8Encoding(false, true).GetString(bytes);
        string hash = Convert.ToHexString(SHA256.HashData(bytes));
        return new(path, json, hash, bytes);
    }

    public async Task<string> BackupAsync(LegacyContactFile file, CancellationToken cancellationToken = default)
    {
        string directory = Path.Combine(_applicationDataDirectory, "LegacyBackups");
        Directory.CreateDirectory(directory);
        string sourceKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.SourcePath)))[..12];
        string backupName = $"{Path.GetFileNameWithoutExtension(file.SourcePath)}-{DateTime.UtcNow:yyyyMMddTHHmmssfffffffZ}-{sourceKey}.json";
        string backupPath = Path.Combine(directory, backupName);
        await using FileStream stream = new(backupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await stream.WriteAsync(file.Bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        return backupPath;
    }
}
