using PhiliaContacts.Business.Modules.Contacts;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Integrations.LegacyContacts;

internal sealed class ContactJsonExportService : IContactExportService
{
    private readonly IContactService _contacts;

    public ContactJsonExportService(IContactService contacts)
    {
        _contacts = contacts;
    }

    public async Task ExportAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        string fullPath = Path.GetFullPath(destinationPath);
        if (File.Exists(fullPath))
        {
            throw new IOException("The export destination already exists. Choose a new file name.");
        }

        string temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, await _contacts.ListAsync(cancellationToken),
                    new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, fullPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
