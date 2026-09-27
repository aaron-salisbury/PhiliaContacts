# Philia Contacts 2.0 roadmap

Status: Phase 0 inventory complete on `phase-0-inventory` (branched from `avalonia-convert`); environment-dependent migration and Store update checks are assigned to Phases 2 and 5. See [Phase 0 compatibility inventory](phase-0-compatibility.md). This is a personal Aaron Salisbury release, independent of Runneth Over Studio. The old UWP application remains the reference implementation until its behavior and user data are covered.

## Release definition

A 2.0 release preserves existing Store customers' contacts, provides a usable Windows and Debian-family desktop app, and retains trustworthy vCard import/export. The Microsoft Store update path and migration must pass an installed-1.x-to-2.0 rehearsal before public submission. Scope new features to those that make contact management reliable and pleasant; defer cloud synchronization and mobile apps.

## Phase 0 — Inventory and compatibility contract

- Audit the UWP projects, contact fields, defaults, settings, photo representation, import/export behavior, existing test data and Store identity. Record a field-by-field mapping and expected behavior for contacts, multiple phone numbers/emails, addresses, notes, favorites and images.
- Inventory legacy storage paths from source: UWP LocalState, `PhiliaContacts.json`, first-release `Contact.json`, and the user-selected folder tracked by a Windows FutureAccessList token. Confirm the 1.0.6.0 file and path from an actual saved sample, and add anonymized representative fixtures; validate 1.0.8.0 saves and custom-folder behavior during Phase 2.
- Capture the latest *published* Store package identity and version in Partner Center. Partner Center screenshots confirm submitted package version `1.0.8.0`, identity name `60826AaronSalisbury.PhiliaContacts`, publisher `CN=7DEA5566-0BC8-4D89-BAB5-AA36A27E4938`, package family `60826AaronSalisbury.PhiliaContacts_gc14fakmyh3dc`, and Aaron Salisbury as publisher. The UWP manifest uses application ID `App`; GitHub's last release is 1.0.7.0. Check live rollout and any later submissions in Partner Center before packaging.
- Write acceptance cases for import, edit, save/restart, export/reimport, update in place, recovery and Linux launch. Use synthetic contacts that exercise non-ASCII names, multiline notes, multiple values, photo formats and malformed input.

**Gate: complete.** Source data formats and package identity documented; anonymized 1.0.6.0-shaped and synthetic later-release fixtures added without private contact data. Actual 1.0.8.0 saves and custom-folder behavior are Phase 2 checks; a packaged Store update is a Phase 5 check.

## Phase 1 — Establish the product solution

The `src` solution, layer projects, Avalonia shell and composition root already exist on `avalonia-convert`. Contact behavior is not ported, and copied Helm domain/data code remains. Design the contact schema and migration contract here as needed to make the business and data boundaries concrete; Phase 2 implements and validates the migration.

- Keep `src/PhiliaContacts.slnx` and the Helm-shaped Business, Data, Integrations, Presentation.Desktop, DesktopApp and Tests projects. Keep the executable as composition root, business contracts in Business, SQLite/Dapper in Data, and platform or external implementations in Integrations. Use the established .NET LTS, DI, migration and test conventions.
- Remove copied Helm modules that do not serve contacts: Goals, LifeDomains, Projects, Relationships, CalDAV, WebDAV and credential infrastructure. Retain useful scaffolding only after adapting names, database paths, registration, navigation and tests. In particular, remove remaining `HelmDatabase` and `HelmDataInitialization` identifiers and migrations.
- Define contact domain types and operations in Business (including stable contact IDs and independent child value IDs where useful). Model repeatable phone/email/address values, favorites, photos, display names and groups without leaking Avalonia controls, UWP objects or database rows across boundaries. Put provider interfaces there; put vCard, file/location and platform implementations in Integrations; keep persistence mappings in Data.
- Build the new application in isolation while keeping the UWP project for comparison. Delete the old root `.sln` and root-level project folders only after the new solution and migration have passed release gates. Preserve history in Git.

**Gate:** new solution builds cleanly with no borrowed Helm domain behavior or root-solution dependency.

## Phase 2 — Storage and migration before routine editing

- Implement a normalized SQLite schema and versioned migrations for contacts and repeatable fields, through Dapper. Define explicit photo storage and limits, a predictable per-user app-data path, transactional writes and failure recovery.
- Create an idempotent legacy importer for both JSON filenames and known schemas using `System.Text.Json` with explicit legacy DTOs or `JsonDocument`; do not add Newtonsoft.Json to the new solution merely to read old files. Preserve the legacy numeric enums, PascalCase fields, date representation and base64 photos through compatibility tests. Read without deleting the source; make a timestamped backup before import; validate counts and mapped fields; commit to SQLite atomically; record completed imports to avoid duplicates. An empty or invalid source must never overwrite populated data.
- On Windows, detect data from the existing UWP package under the same package identity. Investigate access to old LocalState from the new full-trust MSIX and the historical custom-folder token. A token may be unavailable to a desktop process; offer an explicit “Import older Philia Contacts data” file/folder picker and clear recovery instructions as fallback. Test upgrades without uninstalling the old Store package.
- Test upgrade from each real storage variant, repeat launch, partial/corrupt JSON, duplicate contacts, images and rollback from an interrupted migration. Verify backup/restore and readable export independent of SQLite.

**Gate:** 1.x data survives an in-place update and all supported migration variants; failures retain the original source and explain recovery.

## Phase 3 — Contact operations and Avalonia UI

- Port browse, search, detail, add/edit/delete, favorites, settings and import/export to Avalonia MVVM; build keyboard and screen-reader-friendly interaction, theme behavior, sensible empty states and responsive large-list handling. Ensure a new-contact editor does not reuse mutable collections or child records from the previously edited contact.
- Make adding multiple phone numbers obvious and verify editing an existing contact works ([#1](https://github.com/aaron-salisbury/PhiliaContacts/issues/1), closed after it could not be reproduced). Reproduce or explicitly rule out cross-contact sharing and deletion ([#3](https://github.com/aaron-salisbury/PhiliaContacts/issues/3)); test saving and reopening independent records.
- Add phonetic given/family names with compatible vCard import/export mapping ([#4](https://github.com/aaron-salisbury/PhiliaContacts/issues/4)). Add named contact groups only after data and navigation design are settled; #3 also requests folders for home/work/other uses. Decide whether groups meet that use case without tying contact identity to a single folder.
- Revisit photo handling, including source format, serialization and Android round-trip ([#2](https://github.com/aaron-salisbury/PhiliaContacts/issues/2)). Validate actual phone imports with sample JPEG/PNG photos and do not treat a visually similar placeholder as evidence of a stored photo.
- Replace the hand-built vCard writer/parser boundary with tested, standards-aware behavior, or thoroughly fix escaping, UTF-8 octet folding, line endings, photo encoding, repeated fields and unknown-field preservation. The current importer uses `HashSet<Contact>`; explicitly design deduplication and preview/merge rather than relying on reference equality.

**Gate:** all four issues are tracked to a verified test or a documented decision; users can safely round-trip representative contacts with major contact apps.

## Phase 4 — Build and CI

- Add Cake.Frosting under `src/build/Build`, aligned with AppToolkit/UtilityTemplates: Restore, Build, Test, Publish and package tasks, with deterministic version input and artifacts under `artifacts/`. Keep platform-specific packaging behind separate tasks.
- Add GitHub Actions pull-request/push validation for restore, build and tests on Windows and Linux; include packaging smoke checks and artifact retention as appropriate. Pin SDK/tool versions, use least-privilege permissions, and avoid real contact data in artifacts.
- Add a manually initiated release workflow: explicit version/tag validation, build and test gates, Windows and Linux artifacts, hashes and draft GitHub release. Separate Store submission from creating a GitHub release. Avoid silently uploading or submitting packages merely because a tag was pushed.

**Gate:** CI builds and tests the new solution; a manual dry run produces installable, traceable release candidates.

## Phase 5 — Windows Store replacement investigation and proof

- Package the Avalonia Windows build as a **full-trust MSIX** (for example via a Windows Application Packaging Project or manual MSIX tooling). A normal unpackaged `dotnet publish` output is not itself the replacement Store package. Test Windows x64 first; add arm64 only when tested.
- In Partner Center create an **update submission for the existing product** (Store ID `9MXHT996K5ST`). Preserve the exact package name and publisher, use a version greater than every distributed package, and check the existing application ID and package-family continuity. Keep `PublisherDisplayName` as Aaron Salisbury. Confirm Partner Center accepts the switch from UWP to a desktop full-trust package and run a limited rollout before broad release.
- Test installed 1.x → packaged 2.0 on a clean Windows profile, with LocalState and a custom folder, including Store install/update, launch, migration, uninstall/reinstall expectations, file associations and signing. If Partner Center rejects the update or package continuity fails, stop and investigate with a private submission; do not quietly publish a second listing and strand existing customers.

**Gate:** Partner Center accepts the replacement package and a real in-place update preserves customer data. Microsoft's MSIX update documentation says package updates occur within the same package family, so this is a compatibility requirement, not just a naming preference.

## Phase 6 — Linux distribution

- Ship a self-contained `linux-x64` `.deb` in GitHub Releases, with icon, desktop entry, declared native dependencies and sensible XDG data/config paths. Validate install, launch, update and remove on supported Ubuntu and Debian/Mint versions; consider arm64 after testing.
- Document `sudo apt install ./philia-contacts_<version>_amd64.deb` for a downloaded package. Bare `sudo apt install philia-contacts` requires the package to be in an APT repository configured by the user (or accepted into a distro repository); a GitHub Releases attachment alone does not provide that. Evaluate a signed, hosted APT repository later if automatic APT updates justify maintaining repository metadata, signing keys and hosting.
- Evaluate an AppImage as a portable alternative, not a prerequisite for 2.0. Avalonia's packaging docs cover `.deb`; its Parcel tool can generate packages, but is separately licensed professional tooling. Prefer reproducible open tooling if that better fits this project's FOSS goals.

**Gate:** a clean Debian-family machine can install the release artifact and upgrade without losing data.

## Phase 7 — Product polish, listings and release

- Evaluate [Fossify Contacts](https://github.com/FossifyOrg/Contacts) as product inspiration: fast search, configurable visible fields, groups/favorites, batch actions, backup/export, sorting, themes and privacy. Prioritize local-first search and reliable backup for 2.0; stage bulk actions, duplicate review/merge and optional sync as separate follow-up proposals. Fossify is Android-focused; do not assume its device features transfer to a desktop app.
- Refresh `StoreListings/` with a true 2.0 Windows screenshot, updated description, supported-device claims, privacy/support information and Aaron Salisbury attribution; remove outdated UWP/mobile implications. Update README with supported platforms, download/install steps, migration guidance, screenshots, license and attributions. Verify third-party font/icon/photo usage and credits.
- Run beta and rollback rehearsals on Windows and Linux. Publish release notes that state data location, migration and backup behavior, vCard compatibility and known limitations. Create and verify the Store submission separately from GitHub release publication, then monitor issue reports.

**Gate:** screenshots represent the shipped UI; release assets, documentation and live Store listing agree.

## References and decisions to validate

- [Microsoft: package a desktop app with MSIX](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)
- [Microsoft: MSIX app package updates](https://learn.microsoft.com/en-us/windows/msix/app-package-updates)
- [Microsoft: publish an update to an existing Store app](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/publish-update-to-your-app-on-store)
- [Microsoft: packaged desktop app behavior and data redirection](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes)
- [Avalonia: Linux desktop deployment](https://docs.avaloniaui.net/docs/deployment/linux)
- [Avalonia: Parcel packaging and licensing](https://docs.avaloniaui.net/tools/faq)
