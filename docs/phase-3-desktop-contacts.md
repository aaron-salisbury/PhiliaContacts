# Phase 3 desktop contacts

The Avalonia contact screen now uses a searchable, keyboard-selectable list with a separate edit snapshot, buttons for repeatable phone/email/address entries, photo selection and preview, favorite editing, JSON recovery export, explicit older-data import, and vCard preview/selection. The list is backed by the SQLite contact service. The editor copies child entries and photo bytes when opening a contact; changing one draft cannot mutate another persisted contact. Deletion asks for confirmation. The theme follows the OS by default and can be changed in Settings for the current session.

The existing Avalonia controls and the already referenced `Avalonia.Controls.DataGrid` package are sufficient; no additional UI control package is introduced. Picker calls stay in View code-behind and pass a selected path or stream into the ViewModel. The older-data picker accepts the two supported legacy filenames; the import service backs up before writing. If two legacy sources overlap, import one at a time after comparing them. A change to a legacy file after import is reported as `ChangedSource` and not merged automatically.

## vCard boundary

The `VCardContactService` writes vCard 3.0, including CRLF line endings, UTF-8 folding at 75 octets, escaped text, multiple phones/emails/addresses, phonetic name extensions, and JPEG/PNG photo bytes. It accepts common vCard 3.0/4.0 text fields and base64/data URI photos. It retains otherwise unmapped single-line properties through SQLite for subsequent export. Preview marks likely duplicate name-plus-email contacts unchecked and lets users select each entry. Contact IDs remain distinct; no reference-equality deduplication or implicit merging occurs.

This is deliberately a narrow interchange implementation. It rejects quoted-printable vCard data, non-3.0/4.0 versions, photo URLs, and unsupported encodings rather than silently misreading them. The maintained MIT-licensed [FolkerKinzel.VCards](https://github.com/FolkerKinzel/VCards) library supports broader version coverage and is a candidate if real-device samples require it. The current adapter needs actual Android/iOS round-trip verification before the vCard gate is closed.

## Issue decisions and verification

| Issue | Handling | Pending check |
| --- | --- | --- |
| #1 multiple phone numbers and editing | Explicit Add phone number button and independent editor rows | Visual and keyboard check on Windows/Linux |
| #2 photo imports and Android | Preserve exact JPEG/PNG bytes, preview selected photos, export detected MIME format | Import a real Android-generated JPEG/PNG vCard and round-trip on a device |
| #3 cross-contact sharing/deletion and folders | Snapshot editor copies child values; deletion targets a typed ID with confirmation. Named groups deferred until the data/navigation design is proven; a contact may belong to multiple groups in a later schema | Save/reopen two contacts with different child values in the desktop UI |
| #4 phonetic names | Add typed properties and version 3 database migration; map common `X-PHONETIC-FIRST-NAME`/`X-PHONETIC-LAST-NAME` fields | Check Android/iOS phonetic field round-trip |

The Phase 3 feature gate remains open until the desktop UI and external vCard round-trip have been tested on target systems. `docs/roadmap.md` retains the Store update gate for Phase 5.
