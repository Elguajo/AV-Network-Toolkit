# DATA-01 — implementation and isolated verification

2026-11-02, Asia/Tashkent. Owner approved the [contract](../spec/DATA_STORAGE_MIGRATION_CONTRACT.md) at proposal source `36775f2`. Work directly on `main`; Phase 07 remains AWAITING_REVIEW. Original investigation source `a75a3ac`, concurrent accepted updater `bebaa10` and device-password `b837ec6` preserved. Containing implementation commit is reproducible with `git log -1 --format=%H -- app/Services/UserStorage.cs`; final evidence is pinned below after exact-source validation.

## Result and behavior

- Normal startup resolves one immutable `%USERPROFILE%\.av-network-toolkit` context before localization/theme/repositories. Installer and ZIP share it. Worker/self-test dispatch and debug demo override bypass migration. No legacy automatic importer runs after home selection.
- First migration shows source, destination, independent backup, counts, old-build divergence/recovery limitations and exclusions. Copy/Exit and Retry/Exit use a small existing-style Fluent/Mica surface. Close/Exit cancels before normal startup. Pure fresh install creates no invented profiles/preferences.
- All selected bytes, including orphan adapter identities, unknown keys/files, history and private journal association, are copied without reserialization. Original LocalAppData/EXE sources remain unchanged. Unique current-user-protected snapshots live outside the active root; orphan temporaries/locks remain evidence, not active files. Update cache, explicit project files and exports stay outside migration.
- Source inventories, staged files, snapshot bytes/manifest and supported root metadata are checked before no-overwrite same-volume publication. Partial snapshots/staging are retained and never reused. Initialization mutex serializes new bootstraps. Old/noncooperating writers are not controlled; changes detected at startup require explicit Keep home acknowledgement and another snapshot, with no merge/reverse sync.
- Active data format damage blocks cutover; invalid journal evidence remains independently readable/counted under the existing policy. Expired historical snapshots are not mistaken for live requests. Unknown/future root/layout/update versions are preserved. Targets require NTFS; unsupported links/UNC/overlap and inaccessible sources fail closed. The first migration/acknowledgement requires an ordinary user; an unchanged committed root retains existing elevated/ordinary startup behavior.
- App/scanner object preference writers now retain corrupt originals, unknown keys, unique backups and revision conflicts. Draft settings survive failed saves, with deferred localized feedback. This also avoids synchronous UI notification while holding a settings lock. Root loss cannot select LocalAppData or produce a replacement empty dataset; required journal persistence still precedes mutation.

## Source and tests

Main implementation: [UserStorage](../../app/Services/UserStorage.cs), [root selection](../../app/Helpers/AppPaths.cs), [startup](../../app/Services/StorageStartup.cs), [object preferences](../../app/Services/PreservedObjectFile.cs), existing profile/history/layout/journal consumers and [review window](../../app/Views/StorageReviewWindow.xaml).

[UserStorageTests](../../test/UserStorageTests.cs) uses GUID-named synthetic temp roots; both data/recovery roots are isolated. [AppPathsTests](../../test/AppPathsTests.cs) no longer creates a real LocalAppData directory or `C:\Temp\foo`. Tests cover source-byte preservation, all stores, large opaque/oversize/invalid UTF-8 data, malformed/future formats, orphan identities, recovery pointers/expiry/damage, source deletion/divergence, cancelled consent, each interruption boundary and restart, late backup/staging/manifest/record corruption, live file leases, competing initializer threads, NTFS ACL owner/protection, read-only source/injected access failure, actual non-elevated temp junction rejection and overlapping paths.

The WPF harness adds `--user-storage` for modal Copy/Exit/error/divergence, initial Exit focus, accessible action names, visible fixed footer and 12 Light/Dark normal/minimum captures. It never calls ordinary App startup or real network/registry/browser APIs. Two captures were visually inspected: Dark/minimum copy and Light/normal divergence; scrolling retains readable paths and reachable actions. Existing component regression was also run.

The guarded [package runner](../../scripts/test_packages_on_runner.ps1) now prepares a real-resolver synthetic migration via `--prepare-package-storage` before runtime discovery isolation. That helper rejects non-hosted/non-Windows execution and pre-existing user-data roots/files; no marker is forged and no migration-dialog acceptance is automated. Future disposable install/reinstall/uninstall checks retain hashes of legacy, home, recovery and independent snapshot fixtures. This script was not executed locally; syntax and rejection guard are checked separately. No local installer/runtime-root operation is permitted.

## Verification log

Private SDK: `$dataSdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`; private Python: `$dataPython = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. Current working-tree runs used isolated `--artifacts-path out/Data01/artifacts`, preserving other agents' build outputs. Final exact-source checks/results follow in the evidence update.

| Check | Observed result before final exact-source rerun |
|---|---|
| Release solution and WPF harness builds | PASS, 0 warnings/errors after correcting the initial local build invocation and ACL overload |
| Focused DATA-01/root tests | 44 PASS / 0 failed / 0 skipped before three final size/privilege tests |
| Full current working-tree unit suite | 876 PASS / 0 failed / 0 skipped before final backup revalidation/runner changes; not an exact committed-source claim |
| `ProjectUiSmoke ... --user-storage` | PASS, 12 captures, 0 binding errors, real modal handlers/focus/accessible names/footer |
| Existing default `ProjectUiSmoke` | PASS, 0 binding errors; project/device/apply/recovery/link-local/adapter/mDNS/workspace fake-boundary checks |

Intermediate failures retained: initial shared `BaseIntermediateOutputPath` invocation had missing project.assets.json; switched to SDK artifacts isolation and restored existing unchanged dependencies. Initial `Directory.CreateDirectory` ACL overload was invalid for .NET 10 and corrected to `DirectoryInfo.Create(DirectorySecurity)`. Test fixture enum accessibility/xUnit data casts were corrected; blocking task use in a deterministic mutex test was replaced with a bounded second thread, without suppressing analyzers or assertions. These were preparation/compile failures, not ignored acceptance failures.

## Review and remaining limits

Code/reliability/concurrency review traced settings locks/notifications, root-init mutex, source inventory races, snapshot/staging/metadata publication, interruption/retry and all default journal consumers. Review caught and fixed missing final backup revalidation and synchronous notification-under-lock risk. Existing file-level conflict protection remains; no new single-instance product restriction. No production dependency, project schema, adapter identity, network settings/traffic, auth behavior or updater trust policy changed by DATA-01.

Native personal-data migration, ordinary installed-app first-run acceptance, physical power-loss/disk-removal/real disk-full recovery, hosted installer/reinstall/uninstall execution for this source and native UAC/network/DPI acceptance are **NOT RUN**. Fault injection and actual temp-directory ACL/junction tests are not substitutes for those environments. Snapshots consume disk space and are not automatically removed. Old builds keep using LocalAppData and cannot see subsequent home edits; no downgrade synchronization guarantee. No USB-local mode, Phase 08, phase acceptance, install, push or publication. Published corrected 1.1.5 remains `9e06c48` and has none of this migration.
