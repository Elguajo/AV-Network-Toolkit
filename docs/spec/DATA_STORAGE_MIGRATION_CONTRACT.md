# DATA-01 — user-home storage migration proposal

2026-11-02, Asia/Tashkent. **DRAFT / OWNER_REVIEW_REQUIRED; migration NOT IMPLEMENTED.**

Inspected fork: https://github.com/Elguajo/AV-Network-Toolkit, `main`, HEAD `a75a3acb4c533570ba3ce705e012011681d6e63e`. The corrected published 1.1.5 source remains `9e06c48b4b433e0463c2376ba4555c44804ea010`; later documentation is not a new binary. Phase 07 remains AWAITING_REVIEW. ADR-042 approves the future destination, not this transition contract. This chat authorizes investigation and local preparation; actual personal data must not be moved, deleted or overwritten during it.

## Observed storage map

The inventory below comes from source, not inspection of the owner's personal files. `L` means `%LOCALAPPDATA%\NetworkProfileManager`; `E` means the running EXE directory. Installer and ZIP currently use the same `L` locations.

| Store / owner | Current location and format | Migration invariant |
|---|---|---|
| Local profiles — [ProfileRepository](../../app/Services/ProfileRepository.cs) | `L\data\profiles_<sanitized adapterId>.json`; Newtonsoft array of [IpProfile](../../app/Models/IpProfile.cs), maximum five on save | Preserve filename and exact bytes, profile IDs, DNS/gateway/empty-field semantics and ordering. No adapter remapping. |
| Local history — [HistoryRepository](../../app/Services/HistoryRepository.cs) | `L\data\history_<sanitized adapterId>.json`; [HistoryEntry](../../app/Models/HistoryEntry.cs) array, newest first, append retains 100 entries | Copy all existing entries without trimming during migration. Keep nullable `RecoveryOperationId` unchanged. Existing deliberate Clear deletes only this history file. |
| Application settings — [AppSettingsStore](../../app/Helpers/AppSettingsStore.cs) | `L\data\app_settings.json`; flat string dictionary, Theme/Language/HideHyperVUplinks and unknown keys | Preserve every key and byte; no normalization of legacy `tr` on disk. Current writer is best-effort/direct and can replace damaged settings on a later Set. |
| Scanner settings — [IpScannerViewModel](../../app/ViewModels/IpScannerViewModel.cs) | `L\data\scanner_settings.json`; object with range, timeout, parallelism, ARP/TCP preferences and monitor interval | No stored running state; copying settings must never start a scan. Current writer is best-effort/direct. CSV results go only to an explicitly selected export path. |
| Window layout — [WindowLayoutStore](../../app/Services/WindowLayoutStore.cs) | `L\data\window_layout.json`; private version-1, one-record array, 64 KiB initial read limit | Preserve existing invalid/future-version save blocking and monitor/work-area restoration. No new window/adapter/test restoration. [Contract](NAVIGATION_FOLLOWUP_CONTRACT.md). |
| Recovery — [ProjectApplyJournal](../../app/Services/ProjectApplyJournal.cs) | **`L\apply-recovery`**, outside `AppPaths.DataDirectory`; GUID `.request.json` / `.outcome.json`, private typed v1 wire records, 256 KiB limit per record | Move root selection together with data; preserve GUID filenames, request/outcome association and all original snapshots. Current directory writes set a current-user-only protected ACL. |
| Preservation files — [LegacyJsonFile](../../app/Services/LegacyJsonFile.cs), [ProjectStore](../../app/Services/ProjectStore.cs) | Unique `.bak`, temporary `.tmp`, ephemeral `.lock` sidecars | Preserve all backup/orphan temporary bytes as evidence; never infer that a temporary file is committed data. Do not activate/copy a live lock. No automatic retention cleanup. |
| Old EXE-relative import — [AppPaths](../../app/Helpers/AppPaths.cs) | `E\data\*.json` plus `E\app_settings.json` copied into `L\data`; `.migrated_v1` suppresses another import | Existing import copies missing names, does not overwrite/delete sources and retries failed copies. Its marker says nothing about user-home migration. Do not simply point this importer at the new root. |
| Explicit projects — [ProjectStore](../../app/Services/ProjectStore.cs), [ProjectViewModel](../../app/ViewModels/ProjectViewModel.cs) | User-chosen local `*.avnet.json` path; strict project JSON v1; adjacent project backups | **Stay at their selected locations.** No recursive project search, automatic relocation, new relative paths or rewriting of project IDs/device links/adapter hints. Profile JSON exports and scanner/mDNS CSV exports also stay put. |
| Windows startup — [StartupHelper](../../app/Helpers/StartupHelper.cs) | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\NetworkProfileManager`; EXE path plus `--minimized` | Outside DATA-01; no registry identity/path-policy change. Theme/language storage migration is not startup registration. |
| Ephemeral state / tools | Scanner/mDNS/test results and project drafts live in memory; [demo](../../app/Helpers/DemoMode.cs) uses a debug-only temporary override; [self-test](../../app/Services/PackageSelfTest.cs) writes an explicitly supplied create-only report | No session resumption, cache invention or migration in worker/self-test/demo modes. Test overrides must isolate **both** data and recovery roots. |

The normal startup order in [App](../../app/App.xaml.cs) imports old EXE data before localization/theme reads and before constructing the main window. Self-test/elevation worker dispatch precedes that path. A root decision must occur before **any** ordinary settings reader/writer, including localization and shutdown layout persistence.

### Identity and recovery compatibility

[NetworkService](../../app/Services/NetworkService.cs) assigns `AdapterInfo.Id = nic.Id`. Profile/history filenames use the existing regex `[^\w\-] -> _`; no canonicalization, GUID reformatting, filename rename or new identity algorithm belongs in DATA-01. Orphaned/disconnected adapter files survive even when the adapter is not enumerated.

[LocalProfileApplyService](../../app/Services/LocalProfileApplyService.cs) resolves a history pointer to the exact journal request and rejects a mismatching GUID. Typed [ApplyAdapterIdentity](../../app/Models/ProjectApply.cs) includes GUID, current interface index, alias and physical address; [coordinator](../../app/Services/ProjectApplyCoordinator.cs) and worker recheck identity/state. Copying a file does not rebind an adapter, refresh an old index or authorize recovery. Old history without a pointer retains explicitly limited restoration. A pre-existing missing/corrupt request cannot gain a recovery guarantee through migration. Keep the [A26 recovery contract](AUDIT_FIXES_CONTRACT.md).

### Concurrent work boundary

Uncommitted updater work appeared during inspection: `application_updates.json` under `AppPaths.DataDirectory`, plus a disposable `L\updates` download cache. These are **working-tree observations, not committed/published capabilities** at the anchor above. DATA-01 must re-inventory the accepted updater version before implementation: carry durable preferences byte-for-byte through its existing validator; exclude disposable downloads/installer staging from user-data migration and backups. Do not freeze a second cache design here. A separately appearing device-password proposal is not an implemented store or permission to extend JSON v1.

## Proposed destination and root selection

Resolve `H` for the ordinary current Windows user as `Environment.GetFolderPath(SpecialFolder.UserProfile) + ".av-network-toolkit"` using path combination. Validate a nonempty fully qualified local path; never derive the user from a hard-coded account, working directory, EXE location, elevated installer account or arbitrary environment override. A leading dot is a name, not a request to set the Hidden attribute. Microsoft's general recommendation is application-data folders rather than the profile root; the explicit owner-selected ADR-042 destination is the product-specific exception, not a claim that .NET recommends this layout.

Proposed layout:

```text
%USERPROFILE%\.av-network-toolkit\
    storage-root.json          # private version-1 completion/provenance record
    data\                     # unchanged existing filenames and file formats
    apply-recovery\           # unchanged private typed journal files
```

The root metadata contains product/storage version, transaction ID, source kind, snapshot ID, creation time and the captured legacy-source inventory digest. Its separate snapshot manifest records relative names, byte counts and SHA-256, validation findings and absent stores. No file contents, device URLs, plaintext settings values or secrets enter general logs. Metadata is private compatibility/provenance data, not a new public project schema or a reason to trust recovered network settings. Reject unsupported/duplicate/path-traversing metadata. Do not reapply original snapshot hashes to normal active files after legitimate saves.

One immutable root context is selected before repositories are constructed; all durable stores, including the currently hard-coded journal path, consume it. No dynamic root change while the app or a network operation is running. Test dependency injection selects synthetic roots ahead of production resolution. No product `--data-dir` flag or environment-variable escape hatch in DATA-01.

| Startup state | Proposed behavior |
|---|---|
| `H` absent, no legacy user stores anywhere | Prepare/verify/commit an empty home root; no invented profiles or default settings file writes. |
| `H` absent, authoritative legacy set present | Show source/destination, included stores, exclusions, backup location and compatibility warning; explicit **Copy and use new folder** or Cancel. Never move/delete originals. |
| Valid committed `H`, no legacy changes since accepted snapshot | Use `H` exclusively; no repeat import and no dual writes. |
| `H` exists without supported valid completion record, even if apparently empty | Stop before normal readers/writers. Report conflict; Retry or Exit. Do not adopt, merge, rename, replace or empty it automatically. |
| Both roots contain data but `H` has no valid provenance, or recorded legacy bytes have changed | Stop before normal writes; show the two paths and explain divergence. For a valid `H`, explicit **Keep using home data** can acknowledge the new legacy digest after an additional raw snapshot. No automatic merge or timestamp winner. Otherwise Exit for manual reconciliation. Unrecognized `H` needs a separate reviewed repair, not this acknowledgement. |
| Recognized `H` becomes unreadable, metadata unsupported, or a source cannot be distinguished from inaccessible | Retry or Exit. No fallback to LocalAppData, EXE-relative, TEMP or an empty store. |

Normal bootstrap has no permanent user-selected legacy mode: Cancel/Exit leaves personal files unchanged, and an old build remains available for explicit downgrade. Runtime root/path changes need app exit and restart. Installer and ZIP select the same per-user home root, not one root per installation/version.

### Old EXE-relative sources

Inventory only `E\data` and top-level `E\app_settings.json` for the **running** distribution, not the whole EXE folder or other installations. An existing `.migrated_v1` keeps its historical meaning. LocalAppData is authoritative when its import was completed. If import is still pending and EXE files would add or conflict with LocalAppData, show that condition and stop the home migration; do not silently complete a hybrid import as part of a path change. The user can first finish the old import or approve a separate source-selection repair.

If no LocalAppData user stores exist and only old EXE data exists, the preflight can explicitly select that legacy source, snapshot it and copy it directly to `H`; this is a recorded source kind, not a new USB mode. Existing markers, unknown files and orphan backups are preserved as evidence; the new root record alone commits DATA-01. After home activation, remove the normal call to the old importer so stale `E` files cannot be resurrected when a new active file is missing.

## Copy, backup and commit protocol

1. Run only in normal, non-elevated application bootstrap, before localization/window/store initialization. Migration must not run inside the installer, package self-test, elevated worker or demo. Read-only preflight precedes the user's migration consent; cancellation before consent creates no personal-data snapshot.
2. Acquire a current-user storage-initialization lease shared by new builds. Refuse first migration while another candidate application process/operation is active or its status is ambiguous. Competing new bootstraps wait or report Busy, then re-resolve the committed root; never continue with a pre-lease decision. Normal instances using an already committed identical root retain existing per-file concurrency behavior; this proposal does not impose a new single-instance product restriction. Existing old builds do not honor initialization coordination: require tray Exit of all copies before migration and disclose the limit; never kill processes or elevate to inspect them.
3. Inventory all entries under the selected `data` and `apply-recovery` stores, including unknown files, backups and orphan temporaries. Enumerate explicitly and distinguish missing from access/IO errors; `File.Exists == false` alone is insufficient. Reject reparse points, unsafe/device/UNC/network paths, case aliases and source/destination overlap. Do not follow links or change source ACLs. A filesystem without the required ACL/rename behavior is unsupported for this first migration.
4. After explicit consent, create a unique independent raw snapshot at `%USERPROFILE%\.av-network-toolkit-backups\<transaction-id>`. It is outside `H` and outside the source; never overwrite an existing snapshot. Retain originals as a second recovery source. Apply current-user-only Windows protection to snapshots (including recovery data) and staging; a failure to establish protection aborts. This is a separate copy on the same computer, **not protection against disk loss**.
5. Copy with streaming IO, no JSON reserialization, unique create-only files, flushed writes and destination rehash. Hold source read handles excluding writes/deletes where supported; compare full name/length/hash inventory again before commit. Any new/changed/disappeared file, sharing violation, read/flush/hash/capacity/ACL failure aborts without switching roots. No truncation or silent skipping. Live `.lock` files mean Busy; inactive lock artifacts are recorded but never installed as live locks. Raw snapshots preserve orphan `.tmp`/unknown files; active `data`/journal filenames and all `.bak` files retain their relative names. Do not interpret orphan temporary files as authoritative JSON.
6. Build a unique staging sibling of `H` on **the target volume**, using the verified snapshot. Validate known active formats without invoking mutable global defaults or saves. Profile/history checks match existing readers (including strict UTF-8/non-null arrays/8 MiB limit), not newly invented business rules. Validate settings object shapes and private layout/updater versions. Preserve malformed bytes in the raw snapshot; a malformed active profile/history/settings/layout or unknown private version blocks normal promotion until repaired explicitly. Never auto-load a backup or create an empty replacement.
7. Recovery inspection keeps the existing independent-file behavior: invalid/unreadable-format journal records remain byte-identical, are counted and cannot hide valid requests. Unreadable-at-filesystem-level files abort copying. Parse historical requests using existing snapshot rules, **not live request expiry rules**. Preserve valid outcomes and invalid/orphan outcome evidence without inventing completion. For every valid history pointer, record whether the exact request was available before copying and prove the same status/bytes afterward. A pre-existing unavailable recovery is disclosed; migration cannot repair it or downgrade it to a full-recovery claim.
8. Verify source inventory again, verify snapshot and staged manifests, then flush a versioned completion record inside staging. Recheck target absence and path/ACL safety. Publish by a no-overwrite same-volume directory rename. The final valid `H` plus supported record is the selection point; a loose marker or a partial file copy is never completion. If a destination appears concurrently, leave both intact and report conflict. No per-file live-root merge and no copy/delete fallback for cross-volume directory publication.
9. Only after publication/readback succeeds bind all stores to `H` and construct normal UI. If commit outcome is uncertain, exit bootstrap and re-resolve it on restart; do not continue with cached assumptions. Report success only after every required store and recovery association is verified. Preserve snapshot/legacy paths and validation limitations in the local report.

The lease, source handles and before/after inventory reduce cooperative races; they cannot prove immunity to an arbitrary old/noncooperating writer creating a file immediately after the final check. Retained sources/snapshots and subsequent divergence detection are required. No claim of a transaction across two directories, volumes, other processes or sudden power loss.

### Interruption, retry and rollback

| Failure point | Recovery rule |
|---|---|
| Before consent / during inspection | Source unchanged; no active destination. Retry or Exit. |
| During snapshot or staging | Source and existing roots unchanged. Keep owned partial directories identified by transaction ID; mark incomplete. Next run re-inventories source and starts a new transaction; never appends to or trusts partial copies. |
| Before/after publication with uncertain return | Next run recognizes either no committed `H` or the complete supported `H`. A partial/invalid `H` blocks bootstrap; preserve it for explicit repair. Never create a completion marker just to make startup succeed. |
| Repeat start after success | Use home data; no recopy and no new snapshot for an unchanged legacy baseline. Active files may have legitimately changed since migration. |
| Legacy root changed/deleted or became inaccessible later | Report divergence/unavailability before writes; explicit acknowledgement and a fresh snapshot where readable, or Exit. Do not overwrite either side. A missing legacy root is not permission to discard the recorded backup. |
| User needs downgrade | Exit all builds; old build reads its retained **pre-migration** LocalAppData data. It cannot see subsequent home changes. Export/import selected profiles or explicit project files manually as already supported; no automatic reverse migration, journal merge or round-trip guarantee. |
| Home data damaged after normal use | Existing per-store preservation/recovery applies; never restore the initial snapshot automatically over newer data. A complete snapshot restore is a separate user-reviewed operation after all builds exit. |

No automatic deletion of originals, migration snapshots, backups or partial transactions in v1; display their paths and explain disk use. Space is checked as preflight guidance and all writes still handle exhaustion. Cleanup policy is a later decision, not arbitrary age/count retention.

## Normal storage behavior required with the move

Profiles/history/layout keep their existing leases, revision conflict checks, flushed temporary writes and unique exact-byte backups. Preserve five-profile/100-history **normal-operation** semantics; migrating bytes itself never enforces a new limit. Known data formats and JSON v1 are unchanged.

The direct AppSettingsStore/scanner writers are a material integrity gap for the new failure contract. The bounded implementation must give them preserved, atomic, conflict-aware writes and an unreadable/future-data save block while keeping their existing JSON shapes and settings semantics. Preserve unknown application keys. Report a storage save error visibly without replacing it with success or dropping a draft. Do not fold unrelated scanner changes into this work. Document/test the exact behavior rather than implying A26 already hardened these two writers.

After startup, root/disk loss must not redirect writes or create a new empty store. Do not start a new network mutation unless its required recovery journal can be persisted. Existing typed mutation/verification/recovery ordering remains; storage loss after an actual network operation is still a distinct persistence failure, not proof that network state reverted. Noncritical layout/settings saves may fail with retained data and feedback; closing cannot silently change root. Startup failure uses a small Retry/Exit explanation in the existing style, before ordinary views can write. Read [DESIGN.md](../../DESIGN.md) before implementing these local UI additions.

## Installer, ZIP and old/new build matrix

| Combination | Expected behavior / limit |
|---|---|
| Existing 1.1.5 installer or ZIP | Continues using LocalAppData. Does not discover `H`, storage metadata or new backup policy. ZIP is a distribution format, not USB-local settings. |
| First DATA-01-aware installer or ZIP | Same normal-user migration preflight and home root. Installation/uninstallation does not perform the migration or delete either root/snapshot/projects. Preserve stable AppId/EXE/startup identities. |
| New installed and new ZIP copies | Same user data; initialization lease serializes root decisions and competing migration, while existing per-file conflict checks govern ordinary saves. Worker dispatch remains a separate deliberate process path. |
| Old and new copies | No dual write/sync guarantee. Old build can alter retained LocalAppData; new bootstrap detects recorded-source divergence. Do not claim that the old app is prevented from starting or writing. |
| Oldest EXE-relative build | Its files/import marker remain intact. No guarantee that it sees later LocalAppData/home edits. |
| Another user, Run as different user or another PC | Separate Windows user root/adapter identities. No impersonation, implicit transfer or machine rebinding. An elevated worker never chooses a storage root from its own account. |

Existing [installer](../../packaging/AVNetworkToolkit.iss) installs payload to Program Files, requires admin for install, has no automatic app launch and no user-data deletion section. DATA-01 does not change these behaviors. Disposable runner tests must cover retention of both roots and snapshots; no local product installation or publication is authorized here. Coordinate required future packaging checks with updater work after its committed contract is known; do not edit its package/staging logic in this proposal.

## USB-local option — separate proposal, outside DATA-01 approval

Recommended minimal future opt-in: a deliberately created versioned `portable-data.json` next to the portable EXE, with only product/version/mode fields and **no arbitrary path value**. Valid `mode: "usb-local"` selects `E\.av-network-toolkit`; absent marker selects home mode. A malformed/future marker blocks startup rather than being ignored. Do not infer the mode from drive type, ZIP extraction, writable EXE directory or old `E\data` files. Installer payload must not contain this marker.

Selection priority would be: isolated test/demo context; non-ordinary worker/self-test bypass; valid explicit portable marker (subject to installed-mode guard); otherwise committed home/bootstrap migration. An invalid marker or unavailable selected root is an error, not a signal to fall through. An installed copy rejects the marker instead of creating writable data under Program Files. No in-process mode switch or automatic import/copy from home/LocalAppData into USB; that is a separate explicit operation.

For an initial USB increment, recommend **one machine-bound dataset**, not cross-PC merging or a new roaming identity model. On first use, bind the dataset to the current computer using a separately reviewed stable local identity (not adapter name, drive letter or hostname alone). On another PC, block mutation/recovery and offer Exit/manual project-file use; do not relink old per-adapter profiles or replay history. Explicit project JSON is already the portable data interchange. A useful multi-PC local-profile mode needs a separate decision about per-machine partitions/rebinding; it is not implied by carrying a USB drive.

Removal, permission loss, full disk or read-only media: stop new persistence-dependent actions; retain current root identity/drafts and show the failure. No switch to the host's home root and no delayed silent replay into another dataset. Require restart/revalidation after reinsertion; no drive-letter-only identity assumption. Network mutation already in flight follows the existing observe/recovery contract and reports any missing journal/outcome separately. Do not advertise safe recovery if the only journal was on removed media.

Current recovery journals require Windows ACL protection. **Do not claim FAT/exFAT support** or silently weaken protection: the minimum USB mode would require an ACL-capable local filesystem with verified write/rename behavior. Whether broader filesystems, host-private recovery, multi-PC partitions, startup registration suppression and updater replacement of adjacent user files are required remains a separate USB product/security contract. Do not enable USB-local mode in DATA-01 or advertise generic flash-drive support from this proposal.

## Implementation plan after approval

1. Recheck HEAD/status/concurrent updater preferences and cache; finalize an approved DATA-01 decision record without accepting Phase 07. Introduce an injectable immutable root context for every durable consumer; keep worker/self-test/demo isolated.
2. Implement a separately testable preflight/snapshot/staging/commit resolver with fault hooks at inventory, source read, backup write/flush/hash, stage validation, ACL, metadata and publication. No production-root access in tests.
3. Replace normal bootstrap import with the reviewed root workflow before any settings read; add localized review/error/retry/exit states. Harden only the two existing direct preference writers needed for the stated integrity contract. Connect journals/history without renaming identities or schemas.
4. Run focused tests and full build/unit checks; exercise actual WPF bootstrap/settings/error/close/tray behavior using synthetic roots. Review root loss, initialization concurrency and recovery boundaries; record evidence and limitations. Reconcile accepted updater changes before package tests.
5. Update implementation-coupled guide/data-retention/package examples, TASKS/STATUS and handoff with exact source and results. Prepare a scoped local commit. Personal-data migration, installation, live NIC/UAC, push/publication and phase acceptance retain their separate gates.

### Required acceptance evidence (all runtime cases NOT RUN in this proposal)

| Group | Required synthetic/disposable checks |
|---|---|
| Inventory / compatibility | Empty/fresh install; every known store; absent optional files; disconnected and legacy non-GUID filenames; unknown keys/files; multiple adapters; exact profile/history bytes/order; old and current schemas; EXE-only source; completed/pending `.migrated_v1`; no project/export/registry changes. |
| Both roots / downgrade | Identical/different/empty/unrecognized home; malformed/future metadata; new target appears at commit; stale legacy file edits/additions/deletions; Keep home acknowledgement preserves both snapshots; old build fixture remains unchanged; no dual writes or automatic reverse migration. |
| Damage / recovery | Broken UTF-8/JSON/null/oversize arrays; damaged app/scanner settings; future layout/updater preference; corrupt backup; invalid/orphan/expired historical request/outcome; missing journal pointer before versus after; valid recovery GUID and exact snapshot still available. No OS apply. |
| IO / paths / security | Denied reads/writes/ACL, read-only destination, locked file, disk full, flush/hash mismatch, long/non-ASCII names, reparse/overlap/UNC/device paths, inaccessible versus absent, cross-volume source with same-target-volume staging; no unapproved elevation or data in logs. |
| Crash / concurrency | Inject failure/cancel at every boundary above, restart after each partial state, repeat success, two new bootstraps, old-writer inventory race; prove no partial root becomes active and no original/backup is overwritten. Process-kill/power-loss guarantees require separate disposable native evidence. |
| Runtime / UI / regression | Root lost after startup; save conflict/draft retained; no settings reset; mutation cannot start without a journal; no automatic scan/probe/listener/browser/network change; normal/minimized/tray exit and layout failure; Light/Dark existing style; project v1/profile/history/UAC/scanner/diagnostics regressions. |
| Packages | Same source installer/ZIP select same user root; install/reinstall/uninstall retain LocalAppData/home/snapshots/project fixtures; workers/self-test bypass migration; no USB marker in ordinary packages. Hosted/disposable tests only. |

Nearby test patterns inspected: [storage preservation/fault tests](../../test/AuditStorageRegressionTests.cs), [legacy import](../../test/LegacyMigrationTests.cs), [project atomic IO](../../test/ProjectStoreTests.cs), [layout](../../test/WindowLayoutTests.cs), [history](../../test/HistoryRepositoryTests.cs) and [isolated fixture](../../test/TestDataDirectoryFixture.cs). Existing [AppPathsTests](../../test/AppPathsTests.cs) intentionally resolves/creates the real default directory; replace that dependency with injected folder resolution before claiming a wholly isolated migration suite. Do not run default-path tests against personal storage for this investigation.

## Documentation grounding and observed verification

Context7 `/dotnet/docs` was resolved and queried for directory publication and user-folder resolution on 2026-11-02. Results were insufficiently precise for the API contract; official .NET 10 reference pages were also inspected:

- [GetFolderPath](https://learn.microsoft.com/en-us/dotnet/api/system.environment.getfolderpath?view=net-10.0) and [SpecialFolder](https://learn.microsoft.com/en-us/dotnet/api/system.environment.specialfolder?view=net-10.0): use the current user's OS folder mapping; handle empty/unavailable resolution explicitly.
- [Directory.Move](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.move?view=net-10.0): directory destination existence/different volume/in-use/permission failures are explicit failure cases. Thus stage on the target volume and do not use move-over-existing or cross-volume copy/delete publication.
- [FileStream.Flush](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0): durable-flush API intent informs copied-file flushing; this is not evidence of an all-files power-loss transaction.

Source/runtime versions observed: C# WPF/MVVM, `net10.0-windows`, SDK pin `10.0.403` with latestPatch. No dependency change is proposed. Planning/link/tooling/whitespace results for this documentation change are recorded in [STATUS.md](../../STATUS.md); application build/unit/native/package checks are NOT RUN here. The historical release's 744 passing unit tests do not validate DATA-01. No personal root was inspected or modified.

## Owner decision required

Approve this **home-only copy/snapshot/explicit-cutover** contract before implementation: preserve originals, no automatic merge/sync/reverse migration, fail-closed ambiguous/unavailable roots, exact recovery association, bounded settings-writer hardening and the acceptance matrix above. Runtime first-use consent still precedes personal-data copying. USB stays a separate proposal. An alternative requiring automatic old/new synchronization, automatic conflict winners, arbitrary storage locations or multi-PC USB data is a materially different contract and must be settled first.
