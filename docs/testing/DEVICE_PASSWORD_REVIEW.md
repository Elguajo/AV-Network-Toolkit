# Optional device passwords — local implementation review

2026-11-02, Asia/Tashkent. FR-13 / IDEA-03. **VERIFIED_LOCAL; native clipboard and owner acceptance remain separate.** Fork https://github.com/Elguajo/AV-Network-Toolkit, `main`. Approved [contract](../spec/DEVICE_PASSWORD_PROPOSAL.md); proposal commit e729f3d, isolated implementation baseline 36775f22db2912cbbd1b4cdd23c739a9dece0a4d. Resolve the containing implementation commit with `git log -1 --format=%H -- docs/testing/DEVICE_PASSWORD_REVIEW.md`. Phase 07 remains AWAITING_REVIEW.

## Result and compatibility

- Strict v1 schema/example unchanged. Separate [v2 schema](../../schemas/project.avnet.v2.schema.json) and [synthetic example](../../examples/project.avnet.v2.json) add only version 2 and optional device `password`. Unknown versions/properties and duplicate keys remain rejected. Validation tooling compares the complete v2 schema minus this extension against frozen v1 and proves v1 rejects the v2 example.
- Password is absent when not stored/removed; JSON null/empty/control-bearing/malformed Unicode/over-1024-scalar values are rejected. Quotes, Unicode, non-BMP and spaces round-trip without normalization. The editor does not silently truncate pasted values.
- Explicit opt-in, masked native PasswordBox, named Show/Hide, Copy and Remove. Show/copy do not dirty/save; field clearing alone cannot remove an existing password. Selection/project/tab/view/deactivation handlers clear revealed UI text. Normal automation gets a password peer and empty value, not plaintext. Existing device/project draft/discard behavior retained.
- First password-bearing save requires a separate new create-only v2 path, retaining original v1 bytes. Later save/Save As preserve password/version; removing the final password never downgrades a saved v2 file. Consent names plaintext, old-version incompatibility and backup implications. A malformed overwrite target for v2 is treated as possibly containing old passwords.
- Existing atomic write/revision/backup/lease/race-recovery pipeline retained. Writer independently rejects v2-to-v1 downgrade, in-place v1-to-v2 upgrade and unknown/future structure before temporary write; genuinely malformed-file replacement remains explicitly confirmed and recoverable.
- Device record string output redacted; password stripped before Apply & Open event and defensively at Apply VM construction. Browser receives only original validated URL; no authentication, URL interpolation, worker/journal/diagnostic/CSV/probe field added.
- Clipboard lifetime is process-owned and lazy; unit/GUI verification injects a fake. Native copy renders CF_UNICODETEXT and a random ownership marker immediately. Cleanup after 30 seconds compares marker, sequence and text under one exclusive clipboard lock, then clears. Recopy replaces the timer; stale callback cannot clear its replacement. Busy cleanup can retry at application exit; provider/scheduler failures never expose values. A changed clipboard, including another copy of identical text, survives.

The checked opt-in reflects existing storage and is disabled after enabling; Remove is the explicit way to disable storage. Error/warning/clipboard copy stays in the existing English resource dictionary and product styles. No new dependency, lockfile, network setting, UAC/worker policy or installed/published package changed.

## Verification and source isolation

Private SDK: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`; Python: `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. Windows/PowerShell, .NET 10/WPF. No ordinary application startup, live network, real browser/device, elevation, actual clipboard mutation or installation.

The same checkout concurrently contains unfinished application-update and DATA-01 work. To obtain attributable full regression evidence, exported `git archive --format=tar 36775f2` into ignored `TestResults/DevicePassword/Source`, then overlaid only password-owned source/tests/schema/example. Four shared files were reconstructed with only the password additions: app csproj, English strings, ProjectWindow dialogs and harness Program. No branch/worktree/reset/stash or foreign source edit. Restore used an empty local feed (`Source/offline-source`) and existing global package cache; no dependency download or version change.

| Check | Observed result |
|---|---|
| Initial current-tree solution build, own output `bin/DevicePassword/` | PASS, 0 warnings/errors |
| Focused JSON/store/device/project/password regression | 166 PASS, 0 failed/skipped before final additional cases |
| Final isolated full unit suite | **795 PASS, 0 failed/skipped**; includes 51 password-focused additions to the 744 baseline |
| Isolated harness Release build | PASS, 0 warnings/errors |
| Password WPF harness | **20 captures**, Light/Dark, 1000x850 and 700x540; 0 binding errors. Masked automation, explicit reveal then section-hide (without capturing revealed text), copy/draft/remove/new, reachable actions and untruncated oversized paste checked |
| Existing default WPF regression harness | **128 captures**, 0 binding errors; fake network/socket/process/browser/elevation boundaries |
| Combined dirty-tree solution Release build | PASS, 0 warnings/errors after initial concurrent DATA-01 compile errors were repaired by its owner |
| Final combined dirty-tree full unit run | **876 PASS, 0 failed/skipped** after DATA-01 updated its own root contract/tests. Initial run was 837 PASS/2 FAIL in old AppPaths expectations; no unrelated test changed/skipped by this task |
| Planning/schema/tooling/whitespace | Final commands/results recorded in STATUS handoff; no runtime acceptance inferred |

Exact final isolated commands, from repository root:

```powershell
& $sdk restore TestResults/DevicePassword/Source/AVNetworkToolkit.sln --source "$pwd/TestResults/DevicePassword/Source/offline-source"
& $sdk test TestResults/DevicePassword/Source/test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore --logger 'trx;LogFileName=password-isolated-final.trx' --results-directory "$pwd/TestResults/DevicePassword"
& $sdk restore TestResults/DevicePassword/Source/scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --source "$pwd/TestResults/DevicePassword/Source/offline-source"
& $sdk build TestResults/DevicePassword/Source/scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore
& $sdk TestResults/DevicePassword/Source/scripts/ProjectUiSmoke/bin/Release/net10.0-windows/ProjectUiSmoke.dll TestResults/DevicePassword/IsolatedUi --device-passwords
& $sdk TestResults/DevicePassword/Source/scripts/ProjectUiSmoke/bin/Release/net10.0-windows/ProjectUiSmoke.dll TestResults/DevicePassword/IsolatedRegressionUi
& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/DevicePasswordCombined/
& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build --no-restore -p:OutputPath=bin/DevicePasswordCombined/ --logger 'trx;LogFileName=password-combined-full.trx' --results-directory TestResults/DevicePassword
& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-restore -p:OutputPath=bin/DevicePasswordCombinedFinal/ --logger 'trx;LogFileName=password-combined-final.trx' --results-directory TestResults/DevicePassword
& $py -B scripts/validate_plan.py
& $py -B scripts/test_validate_plan.py
git diff --check
```

Ignored evidence: `TestResults/DevicePassword/password-isolated-final.trx`, `password-combined-full.trx`, `password-combined-final.trx`, `password-source-manifest.json`, `Source/`, `IsolatedUi/`, `IsolatedRegressionUi/`. Final isolated TRX SHA-256: `8ed19ba07ac9e656963c4150f1442479600967f906864b151c7534d9cb976d29`; complete frozen source-manifest SHA-256: `482b04f867e5894521119274ca5c3fef2da3a86f7ca84606626fe634a38c6501`. Final combined TRX SHA-256: `a6841bb7481883a884d9ec6ae82b6647e365f175d2681420514e17b36b2ba803`. Snapshot unit executable contains the final clipboard exit-retry/scheduler cases; UI evidence uses the same production UI, and never calls the clipboard backend.

Initial failures were retained as evidence: malformed escaped surrogate escaped the schema library as InvalidOperationException (now string decoding is sanitized before schema evaluation); a whitespace-password redaction assertion was overly broad (now tests constant DTO redaction); masked native automation legitimately returns an empty string (test now accepts empty or denied, never plaintext). A local Python locale-decoding error was corrected back to UTF-8; unrelated glyphs/path validation restored and diff checked. The UI revision removed native MaxLength after review found its silent paste truncation risk. None of these failures were suppressed.

## Security/storage review and limitations

Reviewed the actual DTO/schema/parser/store/VM/dialog/input/clipboard diff and production consumers; tests exercise all seven injected write-failure stages, source byte identity, exact backups, late replacement, unknown/future/downgrade refusal, absent/empty/null/Unicode/length cases, mixed-version parallel schema validation, reveal/copy dirtiness, search exclusion, event redaction and fake apply/browser. No unresolved password-specific correctness/security finding identified within this scope.

Primary sources consulted for implementation: [WPF PasswordBox source](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/PasswordBox.cs) via Context7; [OpenClipboard](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-openclipboard), [clipboard sequence number](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardsequencenumber), [clipboard allocation/ownership](https://learn.microsoft.com/en-us/windows/win32/dataxchg/using-the-clipboard). Locking prevents other applications modifying contents between comparison and EmptyClipboard; immediate rendering avoids delayed-render sequence assumptions. Actual native clipboard allocation/ownership/timeout/exit/history behavior is **NOT RUN**, and fake tests do not certify it.

Plaintext project/temp/backup/copy and managed-string exposure are intentional; there is no encryption/secure-erasure promise. Clipboard history/cloud/other apps can retain copies, and abrupt termination cannot guarantee cleanup. An old v1-only program rejects v2 on Open but can still explicitly overwrite it with another v1 Save As and its own backup flow. No retroactive old-binary protection or new credential-free downgrade/export is claimed.

The initial combined test failures belonged to concurrent DATA-01; after its contract/test updates the final combined full run passed all 876 tests. This does not mark DATA-01 accepted by this task. Native clipboard/history, manual keyboard/Narrator/high-contrast/DPI, different-PC transfer, ordinary app/tray/exit, actual NIC/UAC/browser/device, packaging and owner acceptance remain NOT RUN. Published 1.1.5 is unchanged; Phase 07/native/phase/publication gates remain separate. Safe owner check: use synthetic credentials in a copy of a project, save to a new v2 path, reopen, verify hidden/show/copy/remove and original v1 preservation; real credentials are unnecessary for acceptance.
