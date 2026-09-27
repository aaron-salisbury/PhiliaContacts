# Phase 1 contact foundation

The new `src` solution owns the Philia Contacts 2.0 foundation. The root UWP solution remains as a reference for the UI and migration phases.

- **Business:** `Contact` is an application-facing snapshot with a strongly typed `ContactId`, repeatable email/phone/address values, and all observed legacy contact fields. `IContactService` validates writes and delegates to `IContactStore`. `ILegacyContactReader` is the external-format boundary. Computed UWP display properties are not persisted.
- **Data:** `CreateContactsMigration` creates contacts and ordered child tables; `SqliteContactStore` uses Dapper and a transaction when replacing a contact and its children. The database initializer uses only the Philia contact migration. The database path remains `PhiliaContacts.db` in the application data directory.
- **Integrations:** `LegacyJsonContactReader` parses old JSON with `System.Text.Json`, maps historical numeric type values, retains unknown values as `legacy:<number>`, preserves the original birthday string and base64-decoded photo bytes, and gives each imported record a distinct ID. It parses provided JSON only. Phase 2 will handle discovery, backups, idempotent import, conflict resolution and source retention.
- **Presentation:** the existing Avalonia shell compiles after removing borrowed integration models and fixing missing references. Contact browsing and editing are Phase 3.
- **Tests:** architecture boundaries, a representative 1.0.6.0-shaped fixture, duplicate records, database round trip and independent deletion are verified. The fixture is synthetic and contains no private contact data.

The `src` solution has no Helm domain or provider code. The old root projects and solution are retained until the replacement, migration and UI work pass their later gates. This is a foundation, not a user-ready 2.0 build.
