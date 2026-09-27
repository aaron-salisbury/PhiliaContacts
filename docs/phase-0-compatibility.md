# Phase 0: legacy compatibility inventory

Phase 0 source inventory complete 2026-09-27 on `phase-0-inventory`, branched from `avalonia-convert`. These observations describe the checked-in legacy source, two Partner Center screenshots, a user-supplied saved file containing one fake contact from release 1.0.6.0, and a subsequent launch of the 1.0.8.0 solution against the same LocalState. No production contacts were inspected.

## Existing product and package

| Item | Confirmed value | Evidence |
| --- | --- | --- |
| Publisher | Aaron Salisbury (personal publisher) | Partner Center identity screenshot; UWP manifest |
| Store ID | `9MXHT996K5ST` | Partner Center |
| Published package | `PhiliaContacts.App_1.0.8.0_x86_x64_arm_bundle.msixupload` | Partner Center packages screenshot |
| Published package version | `1.0.8.0`, neutral bundle, Windows.Universal minimum 10.0.18362.0 | Partner Center packages screenshot |
| Package Identity Name | `60826AaronSalisbury.PhiliaContacts` | Partner Center identity screenshot |
| Package Identity Publisher | `CN=7DEA5566-0BC8-4D89-BAB5-AA36A27E4938` | Partner Center identity screenshot |
| PublisherDisplayName | `Aaron Salisbury` | Partner Center identity screenshot |
| Package Family Name | `60826AaronSalisbury.PhiliaContacts_gc14fakmyh3dc` | Partner Center identity screenshot |
| UWP Application ID | `App` | `PhiliaContacts/PhiliaContacts.App/Package.appxmanifest` |
| GitHub's last release | `1.0.7.0` | GitHub releases; the Store's submitted package is newer |
| Copyright | © Aaron Salisbury | `StoreListings/listingData9MXHT996K5ST-1152921505693350919.csv` |

The next Store package must exceed the installed version, preserve the existing package family and be tested as an in-place update. The screenshot confirms a submission package/version; confirm the product's live availability/rollout and any later private submissions directly in Partner Center when packaging begins. Do not publish under Runneth Over Studio.

## Current solution and Phase 1 overlap

The new `src/PhiliaContacts.slnx` and Business, Data, Integrations, Presentation.Desktop, DesktopApp and Tests projects already exist. There is a renamed Avalonia composition root and navigation shell. `HomeView.axaml` is empty; contact functionality still lives in the root UWP projects. The new Data project still has `HelmDatabase`, `HelmDataInitialization` and migrations for unrelated Helm domains. This scaffold is Phase 1 progress, not a working port. The root `PhiliaContacts.sln` and UWP projects must remain accessible until migration and feature parity are verified.

Some Phase 2 design must happen in Phase 1: a contact schema, mapping and legacy-reader interface are needed to choose business contracts and data boundaries. Treat the phases as dependency-oriented checkpoints, not strict fences.

## Actual local data variants

| Variant | Source and behavior | Required handling |
| --- | --- | --- |
| Later-release default data | `PhiliaContacts.json` in `ApplicationData.Current.LocalFolder`; an array of `Contact` objects serialized by Newtonsoft.Json. A surviving 1.0.8.0 saved file was compared with the original 1.0.6.0 sample; all parsed contact fields and the decoded photo bytes match | Read in place, copy/backup, validate, migrate |
| First public filename | `Contact.json` confirmed from release 1.0.6.0 in UWP LocalState. `ReadReplaceDomainsAsync` only checks this name during a folder-location change | Search without changing/deleting the file; actual 1.0.6.0 field shape now represented by anonymized fixture |
| Custom folder | `AppStorageLocation` in UWP `ApplicationData.Current.LocalSettings` is a JSON-serialized absolute path. `FutureAccessList` stores the chosen folder under `StorageFolderPath.GetHashString()`. A 1.0.8.0 save succeeded in a Documents subfolder, while saving to a custom folder immediately under Local AppData failed with access denied | Offer manual picker; check settings and token access on a packaged update; never assume token is transferable or a chosen path writable |
| UWP settings | `AppStorageLocation`, `AppBackgroundRequestedTheme` in LocalSettings | Preserve useful preferences after verifying actual values; do not interpret these as contact records |
| Imported vCards | External `.vcf` parsed into contacts; not a separate automatic local-storage format | Keep separate from startup migration; test import/export compatibility |

**Important code path:** `StorageLocationService.SaveStorageLocationInSettingsAsync` calls the Manager's `StorageFolderToken` setter. That setter calls `Delete()` on the *old* location before loading the new one; `ReadReplaceDomainsAsync` deletes `Contact.json` before deserializing it and writing `PhiliaContacts.json`. The later 1.0.8.0 experiment below confirms destructive folder-switch behavior. The migration must never call these legacy methods as a way to discover or convert data. Preserve files in both locations and treat conflicting copies as separate sources for user review.

`Manager.Load()` reads only `PhiliaContacts.json`. If it is missing, it presents an empty contact collection even if `Contact.json` exists. **Observed reproduction:** after creating a fake contact in release 1.0.6.0 and saving `Contact.json` to `C:\\Users\\<user>\\AppData\\Local\\Packages\\60826AaronSalisbury.PhiliaContacts_gc14fakmyh3dc\\LocalState`, the publisher closed that solution and launched the 1.0.8.0 solution without changing its settings. The later app displayed the same default LocalState path but its Contacts view was empty. At that first launch, no write or folder change was performed. The publisher reuploaded `Contact.json` afterward; it was byte-for-byte identical to the original upload (126,762 bytes, SHA-256 `c785a5d932e439ee5864deb2c733d92fdf6845dd85c840b65ebf65c5e8fb82fe`). Thus that launch did not alter the file. Subsequent folder changes did alter it; see below. A user-supplied `Contact.json` created by the oldest GitHub release (1.0.6.0) confirms this filename, path and field shape. It contains one fake contact, including computed fields serialized by Newtonsoft.Json. The supplied image is a PNG stored as a base64 JSON string (94,532 decoded bytes), despite the exporter always labeling photos as JPEG. Its timestamped birthday includes time and seven fractional digits, so conversion to a date-only field must be an explicit decision. The first-release fixture below has been anonymized and uses a generated one-pixel PNG; the original file and its personal path are **not** committed.

**Later 1.0.8.0 custom-folder experiment (reported by the publisher):**

1. Changing the save folder to `C:\\Users\\<user>\\AppData\\Local\\PhiliaContacts` left the original LocalState `Contact.json` intact and did not migrate it. A new-contact save failed with `0x80070005 (E_ACCESSDENIED)`, as shown in the application's error log. The cause of this directory's denied access has not been isolated.
2. Changing the save folder to `C:\\Users\\<user>\\Documents\\Philia Contacts` and creating another test contact allowed a successful save. `PhiliaContacts.json` was observed in that Documents directory. The earlier unsaved test contact was gone on returning to its tab.
3. Changing the folder back to the original package LocalState removed `PhiliaContacts.json` from Documents. The original LocalState `Contact.json` disappeared and a new `PhiliaContacts.json` appeared containing only the contact from the old `Contact.json`, not the newly saved Documents contact.

Before this experiment the publisher did not find a `PhiliaContacts.json` in LocalState; when or why an earlier copy went missing is unknown. Folder changes can delete a valid source and leave a different contact set at the new location. The 2.0 importer must only read and copy files; users should make independent copies of every surviving legacy JSON file before further 1.x folder changes.

**Saved-file comparison:** The surviving 1.0.8.0 `PhiliaContacts.json` contains one contact (126,762 bytes; SHA-256 `17c5c558cf8daddb056d0e2ab6757820f1d032a8db5ba5913b2c9dcd6729df1d`). Parsing both uploaded files as JSON yields the same complete contact object: names, ordered phone and email fields, enum values, birthday, notes, flags, and decoded photo bytes all match. The later file places `IsFavorite` first; the original `Contact.json` places it later. The bytes differ because property order differs, not because the older contact was lost. The 1.0.8.0 format is therefore covered by the property-name-based `System.Text.Json` reader. A generated, anonymized fixture replicates the observed property ordering at `tests/fixtures/legacy/current/observed-1.0.8.0/PhiliaContacts.json`. The supplied original is not committed. The deleted Documents file containing the later test contact was not supplied; its content cannot be compared.

## Persisted field mapping

Source: `PhiliaContacts.Domains.Contact`, `EmailAddress`, `PhoneNumber`, historically serialized with default Newtonsoft.Json settings. **The 2.0 migration reader will use only `System.Text.Json`; do not add a Newtonsoft.Json dependency to the new solution.** Use explicit legacy DTOs or `JsonDocument`/`JsonElement` to interpret the observed wire shape, instead of deserializing directly into new domain types. Public calculated properties may also appear in JSON; ignore and recompute them. There is **no persistent contact ID** in the legacy model. The new app must assign stable IDs and keep source ordering/occurrence information during migration rather than merging equal-looking records automatically.

| Legacy JSON field | Shape | Proposed 2.0 mapping and validation |
| --- | --- | --- |
| `GivenName`, `MiddleName`, `FamilyName`, `Nickname`, `Prefix`, `Suffix` | strings/null | Contact name components; preserve empty and Unicode values |
| `FormattedName`, `DisplayName` | computed strings | Recompute for UI; do not trust as source of truth |
| `EmailAddresses` | array of `{ Email, Type }` | Ordered child values with independent identity; preserve duplicates until deliberate merge |
| `PhoneNumbers` | array of `{ Number, Type }` | Same; do not normalize away punctuation or leading `+` |
| `Birthday` | nullable serialized `DateTime`; observed `2000-01-01T13:03:02.7850172` shape | Calendar date; preserve or explicitly discard time after tests and decide how to handle time-zone/yearless dates |
| `Title`, `Organization`, `Url`, `Notes` | strings/null | Preserve text and line breaks |
| `Photo` | `byte[]` (base64 string in JSON), null; observed PNG despite JPEG exporter label | Preserve exact bytes, inspect content type separately; distinguish legacy placeholder from user image |
| `TwitterUser`, `FacebookUser`, `LinkedInUser` | strings/null | Preserve social identifiers, even if UI support changes |
| `AddressType` | enum serialized numerically by default | Single legacy address type, retain original value; no assumption of multiple addresses |
| `Street`, `City`, `State`, `Zip`, `CountryRegion` | strings/null | Single legacy address, mapped to 2.0 address record only when data exists |
| `IsFavorite` | boolean | Favorite flag |
| `FavoriteSegoeMDL2Glyph`, `IsValid` | computed UI/validation properties | Do not persist; recompute or replace |

Compatibility details for `System.Text.Json`: preserve the original PascalCase property names or configure them explicitly; treat absent and null optional properties deliberately; parse the numeric enum values against the historical mapping (including a defined unknown-value outcome); decode `Photo` as base64 and validate bytes/content type; read the serialized `Birthday` without silently shifting its date by time zone. Include a byte-backed 1.0.6.0-shaped fixture, empty arrays, nulls and malformed JSON in migration tests. The serializer that originally wrote the file does not need to be installed to read its standard JSON representation.

Legacy numeric enum values: `AddressType` Work=0, Home=1, Domestic=2, International=3, Postal=4, Parcel=5, None=6; `EmailAddress.Type` Work=0, Internet=1, Home=2, AOL=3, Applelink=4, IBMMail=5, None=6; `PhoneNumber.Type` Work=0, Cell=1, Home=2, Voice=3, Text=4, Fax=5, Pager=6, Video=7, TextPhone=8, MainNumber=9, BBS=10, Modem=11, Car=12, ISDN=13, None=14. Include an unknown-value policy rather than silently remapping out-of-range numbers.

## Existing contact behaviors and risks

- Browse sorts favorites first, then `DisplayName`. `DisplayName` chooses nickname or family name, not full name. The screen exposes selection, new, delete, save, vCard import and vCard export. Deleting a contact removes it in memory; saving writes the *entire collection* to one JSON file.
- A new contact gets the placeholder image bytes. `IsValid` requires at least one of given name, family name or nickname. `Writer` silently skips invalid contacts, so export counts may differ from stored counts.
- Import parses with `EWSoftware.PDI`, then unions a `HashSet<Contact>` with existing records. `Contact` has no custom equality implementation, so equivalent new objects can be duplicated. Import does not itself call `Manager.Save()`; persistence depends on a later save.
- The writer emits vCard 3.0 manually. It does not escape all text/structured values; line folding counts C# characters rather than UTF-8 octets; it writes `PHOTO:TYPE=JPEG;ENCODING=BASE64` regardless of photo bytes and compares byte arrays by reference to suppress the placeholder. It uses `Environment.NewLine`. These details warrant round-trip tests, not an assumption of compatibility.
- The active parser imports one preferred address, first URL/note, selected social identifiers, categories containing `starred`, phones/emails and photo. Photo conversion has special cases for JPEG/base64 and otherwise attempts URL retrieval; errors substitute a placeholder. Alternate `VCardLibVCFService` exists but is not selected by `Importer`.
- Contacts are sensitive personal data. Test fixtures must be fictitious and CI logs must not include contact content.

## Acceptance matrix for the port

| Scenario | Expected result |
| --- | --- |
| First run without legacy data | New empty SQLite database, no import completion marker for absent data |\n| 1.0.6.0 `Contact.json` followed by 1.0.8.0 empty view | 2.0 discovers and migrates the file despite the prior app showing zero contacts; it never interprets the empty view as an authoritative empty source |
| Current JSON in UWP LocalFolder | Non-destructive copy and migration; count, field values, child order and photos match; source remains readable |
| First-release filename | Parse anonymized 1.0.6.0-shape fixture, backup first, do not delete source |
| Custom folder and LocalFolder both contain data | Detect both and offer conflict-aware selection/import; never delete either automatically |
| Relaunch after migration | No duplicate contacts; IDs and data stable |
| Corrupt JSON, invalid enum or partially missing fields | Actionable error and intact source/backup; no replacement of valid destination data |
| Multiple phones/emails, Unicode, multiline notes, birthdays and images | Data survives edit, save, restart and vCard round trip |
| New contact after editing another | No shared mutable phone/email/photo references; changes to one cannot mutate the other |
| 1.0.8.0 Store install updated to 2.0 MSIX | Same product and package family; existing data located or recoverable; Store update and launch succeed |
| Linux fresh install and update | XDG path respected; data retained across upgrades |

## Fixtures and implementation checks

`tests/fixtures/legacy/current/PhiliaContacts.json` is a synthetic fixture matching the visible legacy contact fields and default JSON shapes. `tests/fixtures/legacy/current/observed-1.0.8.0/PhiliaContacts.json` is a sanitized, compact fixture preserving the newly observed 1.0.8.0 field order and the computed fields. `tests/fixtures/legacy/first-release/Contact.json` is an anonymized, structurally faithful fixture informed by a user-supplied 1.0.6.0 saved file. It retains every observed property, numeric enum shape, timestamp form, ordered child arrays and base64 PNG photo representation while replacing all entered values. Keep real data out of Git.

The 1.0.8.0 launch against a 1.0.6.0 file has been observed to show an empty view while retaining the default LocalState path; the file remained byte-for-byte intact. The oldest 1.0.6.0 saved-file format, LocalState path and Partner Center identity have been observed. This completes the Phase 0 source inventory and acceptance contract for the earliest format. The later folder-change experiment demonstrates why the original file must be copied before using the legacy app again.

**Required implementation checks:** The 1.0.8.0 file shape and old-contact preservation have been verified. Test old settings and FutureAccessList token access in a packaged update. During Windows packaging in Phase 5, validate an in-place Store update and that a full-trust MSIX with the same package family can access the old LocalState. Keep the Phase 2 and Phase 5 release gates blocked until those respective checks pass.
