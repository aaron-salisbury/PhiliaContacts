using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhiliaContacts.Business;
using PhiliaContacts.Business.Modules.Contacts;
using PhiliaContacts.Data;
using PhiliaContacts.Data.Database;
using PhiliaContacts.Integrations;
using PhiliaContacts.Integrations.LegacyContacts;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhiliaContacts.Tests.Contacts;

[TestClass]
public sealed class LegacyContactImportTests
{
    [TestMethod]
    public async Task ImportBacksUpExactBytesAndRemainsIdempotentAcrossServiceProviders()
    {
        string directory = CreateDirectory();
        try
        {
            string source = Path.Combine(directory, "Contact.json");
            byte[] original = await File.ReadAllBytesAsync(Fixture("first-release", "Contact.json"), TestContext.CancellationToken);
            await File.WriteAllBytesAsync(source, original, TestContext.CancellationToken);
            using (ServiceProvider provider = await CreateProviderAsync(directory))
            {
                ILegacyContactImportService importer = provider.GetRequiredService<ILegacyContactImportService>();
                LegacyImportResult imported = await importer.ImportAsync(source, TestContext.CancellationToken);
                Assert.AreEqual(LegacyImportOutcome.Imported, imported.Outcome);
                Assert.AreEqual(1, imported.ContactCount);
                CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(imported.BackupPath!, TestContext.CancellationToken));
                CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(source, TestContext.CancellationToken));
            }

            using ServiceProvider restarted = await CreateProviderAsync(directory);
            LegacyImportResult repeated = await restarted.GetRequiredService<ILegacyContactImportService>().ImportAsync(source, TestContext.CancellationToken);
            Assert.AreEqual(LegacyImportOutcome.AlreadyImported, repeated.Outcome);
            Assert.IsNull(repeated.BackupPath);
            Contact contact = (await restarted.GetRequiredService<IContactService>().ListAsync(TestContext.CancellationToken)).Single();
            Assert.AreEqual("Historical", contact.GivenName);
            Assert.HasCount(2, contact.EmailAddresses);
            Assert.AreEqual("2000-01-01T13:03:02.7850172", contact.Birthday);
            CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71 }, contact.Photo![..4]);

            await File.WriteAllTextAsync(source, "[{\"GivenName\":\"Changed\"}]", TestContext.CancellationToken);
            LegacyImportResult changed = await restarted.GetRequiredService<ILegacyContactImportService>().ImportAsync(source, TestContext.CancellationToken);
            Assert.AreEqual(LegacyImportOutcome.ChangedSource, changed.Outcome);
            Assert.HasCount(1, await restarted.GetRequiredService<IContactService>().ListAsync(TestContext.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task DiscoveryDefersCompetingNonemptyFilesAndIgnoresEmptyFile()
    {
        string directory = CreateDirectory();
        try
        {
            string first = Path.Combine(directory, "Contact.json");
            string later = Path.Combine(directory, "PhiliaContacts.json");
            await File.WriteAllTextAsync(first, "[{\"GivenName\":\"One\"}]", TestContext.CancellationToken);
            await File.WriteAllTextAsync(later, "[{\"GivenName\":\"Two\"}]", TestContext.CancellationToken);
            using ServiceProvider provider = await CreateProviderAsync(directory);
            ILegacyContactImportService importer = provider.GetRequiredService<ILegacyContactImportService>();
            LegacyImportResult[] choices = [.. await importer.ImportDiscoveredAsync(TestContext.CancellationToken)];
            Assert.HasCount(2, choices);
            Assert.IsTrue(choices.All(result => result.Outcome == LegacyImportOutcome.NeedsSelection));
            Assert.IsEmpty(await provider.GetRequiredService<IContactService>().ListAsync(TestContext.CancellationToken));

            await File.WriteAllTextAsync(later, "[]", TestContext.CancellationToken);
            LegacyImportResult[] results = [.. await importer.ImportDiscoveredAsync(TestContext.CancellationToken)];
            Assert.IsTrue(results.Any(result => result.Outcome == LegacyImportOutcome.Empty));
            Assert.IsTrue(results.Any(result => result.Outcome == LegacyImportOutcome.Imported));
            Assert.HasCount(1, await provider.GetRequiredService<IContactService>().ListAsync(TestContext.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task InvalidAndInterruptedImportsDoNotChangeDatabaseOrSource()
    {
        string directory = CreateDirectory();
        try
        {
            string source = Path.Combine(directory, "Contact.json");
            using ServiceProvider provider = await CreateProviderAsync(directory);
            ILegacyContactImportService importer = provider.GetRequiredService<ILegacyContactImportService>();
            IContactService contacts = provider.GetRequiredService<IContactService>();
            await contacts.SaveAsync(new Contact { Id = ContactId.New(), GivenName = "Existing" }, TestContext.CancellationToken);
            await File.WriteAllTextAsync(source, "[{\"GivenName\":\"Bad\"},null]", TestContext.CancellationToken);
            await Assert.ThrowsExactlyAsync<JsonException>(() => importer.ImportAsync(source, TestContext.CancellationToken));
            Assert.IsFalse(Directory.Exists(Path.Combine(directory, "LegacyBackups")));

            await using (SqliteConnection connection = new($"Data Source={Path.Combine(directory, "PhiliaContacts.db")}"))
            {
                await connection.OpenAsync(TestContext.CancellationToken);
                await using SqliteCommand trigger = connection.CreateCommand();
                trigger.CommandText = "CREATE TRIGGER RejectOther BEFORE INSERT ON Contact WHEN NEW.GivenName = 'Other' BEGIN SELECT RAISE(ABORT, 'injected failure'); END;";
                await trigger.ExecuteNonQueryAsync(TestContext.CancellationToken);
            }
            await File.WriteAllTextAsync(source, "[{\"GivenName\":\"Good\"},{\"GivenName\":\"Other\"}]", TestContext.CancellationToken);
            await Assert.ThrowsExactlyAsync<SqliteException>(() => importer.ImportAsync(source, TestContext.CancellationToken));
            Assert.HasCount(1, await contacts.ListAsync(TestContext.CancellationToken));
            await using (SqliteConnection connection = new($"Data Source={Path.Combine(directory, "PhiliaContacts.db")}"))
            {
                await connection.OpenAsync(TestContext.CancellationToken);
                await using SqliteCommand marker = connection.CreateCommand();
                marker.CommandText = "SELECT COUNT(*) FROM LegacyContactImport";
                Assert.AreEqual(0L, (long)(await marker.ExecuteScalarAsync(TestContext.CancellationToken))!);
                await using SqliteCommand removeTrigger = connection.CreateCommand();
                removeTrigger.CommandText = "DROP TRIGGER RejectOther";
                await removeTrigger.ExecuteNonQueryAsync(TestContext.CancellationToken);
            }
            LegacyImportResult imported = await importer.ImportAsync(source, TestContext.CancellationToken);
            Assert.AreEqual(LegacyImportOutcome.Imported, imported.Outcome);
            Assert.HasCount(3, await contacts.ListAsync(TestContext.CancellationToken));
            Assert.AreEqual("[{\"GivenName\":\"Good\"},{\"GivenName\":\"Other\"}]", await File.ReadAllTextAsync(source, TestContext.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task PopulatedDestinationRequiresReviewAndExplicitImportSupportsReadableExport()
    {
        string directory = CreateDirectory();
        try
        {
            string source = Path.Combine(directory, "PhiliaContacts.json");
            File.Copy(Fixture("current", "PhiliaContacts.json"), source);
            using ServiceProvider provider = await CreateProviderAsync(directory);
            IContactService contacts = provider.GetRequiredService<IContactService>();
            await contacts.SaveAsync(new Contact { Id = ContactId.New(), GivenName = "Existing" }, TestContext.CancellationToken);
            ILegacyContactImportService importer = provider.GetRequiredService<ILegacyContactImportService>();
            LegacyImportResult result = (await importer.ImportDiscoveredAsync(TestContext.CancellationToken)).Single();
            Assert.AreEqual(LegacyImportOutcome.NeedsSelection, result.Outcome);
            Assert.HasCount(1, await contacts.ListAsync(TestContext.CancellationToken));
            await importer.ImportAsync(source, TestContext.CancellationToken);
            string export = Path.Combine(directory, "recovery.json");
            await provider.GetRequiredService<IContactExportService>().ExportAsync(export, TestContext.CancellationToken);
            using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(export, TestContext.CancellationToken));
            Assert.AreEqual((await contacts.ListAsync(TestContext.CancellationToken)).Count, document.RootElement.GetArrayLength());
            await Assert.ThrowsExactlyAsync<IOException>(() => provider.GetRequiredService<IContactExportService>().ExportAsync(export, TestContext.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"PhiliaContacts-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string Fixture(string variant, string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "fixtures", "legacy", variant, fileName);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string directory)
    {
        ServiceCollection services = new();
        services.RegisterInternalDataServices(directory).RegisterInternalBusinessServices().RegisterInternalIntegrationsServices(directory);
        // Tests explicitly control discovery roots regardless of the host OS.
        services.AddSingleton<ILegacyContactFiles>(_ => new LegacyContactFiles(directory, [directory]));
        ServiceProvider provider = services.BuildServiceProvider();
        await provider.InitializePhiliaContactsDataAsync();
        return provider;
    }

    public TestContext TestContext { get; set; }
}
