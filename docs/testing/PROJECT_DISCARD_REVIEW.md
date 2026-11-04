# Themed unsaved-change confirmation — UI-DISCARD-01

2026-11-04, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit; main; inspected clean base `1f96e3526ad95eb8b3ffb69455cf0f257f57be0f`. Phase 07 remains AWAITING_REVIEW. Owner requested fixing the pictured classic dialog, then commit/push and a new local application version. Local 1.1.7 Preview is authorized; no public Release, installation or native network testing.

## Cause, correction and scope

`ProjectDialogs.ConfirmDiscard` used `System.Windows.MessageBox.Show` with Yes/No, Question and default No, bypassing the WPF-UI resources used by other windows. This matches the owner's screenshot.

Replace only this confirmation with `ProjectDiscardDialog`, a small Fluent/Mica window using existing typography, FooterBar, semantic resources and WPF-UI buttons. Discard changes is explicit; primary Cancel starts focused and is the default/cancel action. Enter on opening, Escape, Cancel and title-bar close return false. Focusing Discard changes and pressing Enter deliberately accepts. The active window must belong to the original dialog owner's chain before it can own the confirmation, preserving nested editor z-order and cancellation focus. Only an explicit true modal result allows the existing VM action to proceed.

No VM/store/schema/network/scanner/history/recovery/UAC/tray behavior, dependency, actual data or other MessageBox call changed. Version/README/changelog/local notes are updated to 1.1.7 at the owner's request. This packages previous completed profile/diagnostic/device commits as well; their independent risks and native gates remain in their reviews.

## Checks actually run

Windows/PowerShell; pre-existing private SDK 10.0.403/.NET 10.0.14. Process-local `DOTNET_ROOT = $env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10`. No App.Run/OnStartup, installed app, NIC/probe/socket/elevation, real browser/clipboard or personal-data migration. WPF modes run serially.

```powershell
$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"
& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-restore -p:OutputPath=bin/ProjectDiscard/
& ./scripts/ProjectUiSmoke/bin/ProjectDiscard/ProjectUiSmoke.exe TestResults/ProjectDiscard/FinalFocused --project-discard
& $sdk test AVNetworkToolkit.sln -c Release --no-restore -p:OutputPath=bin/ProjectDiscard/ --logger 'trx;LogFileName=project-discard-117.trx' --results-directory TestResults/ProjectDiscard/FinalUnit
& ./scripts/ProjectUiSmoke/bin/ProjectDiscard/ProjectUiSmoke.exe TestResults/ProjectDiscard/Existing --device-passwords
& ./scripts/ProjectUiSmoke/bin/ProjectDiscard/ProjectUiSmoke.exe TestResults/ProjectDiscard/Regression
```

- Release application/test/harness builds: PASS, 0 warnings/errors. Final full 1.1.7 suite: **977 PASS / 0 failed / 0 skipped**; TRX under FinalUnit. Earlier identical-product 1.1.6-metadata run also 977 PASS.
- New actual-dialog mode: **4 Light/Dark normal/minimum captures**, PASS; clean-project no-prompt, real VM New refusal/acceptance, Cancel/Enter/Escape/close, explicitly focused discard+Enter, accessible names, keyboard reachability, fixed actions, native HWND theme and live theme transitions. Detached profile/device application-exit refusal, actual close/Escape/discard, original data/draft preservation and focus return PASS, including an editor two owners below the service's owner. No binding errors.
- Existing device mode: **52 captures PASS**, fake clipboard/browser/apply. Existing default Project/device/apply/Link-Local/adapter/mDNS/workspace mode: **128 captures PASS**, 0 binding errors. Those product sources are unchanged by the subsequent version bump. Total final distinct capture scenarios: 184.
- Visual QA: inspected all four actual dialog PNGs against DESIGN.md; no clipping, missing content or style discrepancy found. Native Mica is composited by the existing capture helper; HWND theme checked separately. This is not physical desktop/DPI/Narrator/high-contrast certification.
- Scoped code review by the implementing agent: inspected new window, modal/default/cancel handlers, owner-chain selection and existing VM/editor callers; no meaningful defect found. Not independent reviewer certification.

Retained preparation failures: global dotnet had no SDK; used the existing private SDK without installation. First new harness assertion incorrectly expected New project to be clean; existing Load with no snapshot intentionally marks it dirty. Corrected the fixture to assert the default name/empty collections, preserving product behavior. Second failure came from a plain synthetic Window picking up an incompatible late transparency style; changed only that harness parent to FluentWindow, matching the product. Final tests add actual editor-close and focused-discard Enter paths; no gate suppressed. An initial documentation patch matched the wrong decision-log heading and was atomically rejected; reapplied using its actual heading.

## Publication, package and remaining acceptance

Exact implementation/package source **ca7d4754959c8fd75a1c56b5c8236140c68ed34f** committed and pushed to origin/main. Before this task main had ten completed unpublished commits; the owner-authorized ordinary push included them without rewriting history. Existing Git author configuration was absent; used command-local author/committer values matching the last five commits and the authenticated GitHub owner, without changing global/repository settings.

Exported that exact commit with git archive into `out/Local117-ca7d475/Source`. Compared all 215 app/test/harness files against the tested working tree: 155 byte-identical, 60 differ only in CRLF/LF due Git checkout normalization, zero content differences. Initial raw-byte-only comparison was correctly rejected, then explicitly resolved as line-endings-only; not claimed byte-identical. Evidence: `out/Local117-ca7d475/source-comparison.json`.

```powershell
$candidateRoot = Join-Path (Get-Location) 'out/Local117-ca7d475'
git archive --format=zip --output=out/Local117-ca7d475/source.zip ca7d4754959c8fd75a1c56b5c8236140c68ed34f
Expand-Archive -LiteralPath (Join-Path $candidateRoot 'source.zip') -DestinationPath (Join-Path $candidateRoot 'Source')
& (Join-Path $candidateRoot 'Source/scripts/build_packages.ps1') -Dotnet $sdk -Compiler "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/InnoSetup-6.7.4/ISCC.exe" -SourceCommit ca7d4754959c8fd75a1c56b5c8236140c68ed34f -OutputDirectory ../Packages
```

PASS, 0 publish warnings/errors, pinned compiler integrity PASS. Both packages include .NET 10.0.14 and unchanged MIT/third-party notices. Hidden payload and fresh extracted EXE `--package-self-test <new-absolute-json-path>` exit 0: Success/JSON/WPF true, Version 1.1.7.0, informational `1.1.7+ca7d4754959c8fd75a1c56b5c8236140c68ed34f`, Preview, no production updater key. All eight ZIP entries extracted to a fresh ZipVerified directory and SHA-256 matched against the payload. No ordinary application startup or installer execution.

Ready local artifacts:

- EXE with adjacent dependencies: `out/Local117-ca7d475/Packages/payload/NetworkProfileManager.exe`.
- Portable: `out/Local117-ca7d475/Packages/AVNetworkToolkit-1.1.7-win-x64-portable.zip`; SHA-256 `e9709457d76ef0ff2c9e1f08b74a8fce03ef88a76d3b81beb5847cb5de5e1330`.
- Installer: `out/Local117-ca7d475/Packages/AVNetworkToolkit-1.1.7-win-x64-setup.exe`; SHA-256 `adf0819db126367c60a7910a9848ea7d809d396de3f44ac0b6f21e86ebc33edc`.
- Ignored evidence: package-manifest.json, payload-self-test.json, extracted-self-test.json and zip-verification.json beside the packages. Prior local packages/installed app/public 1.1.6 unchanged.

Planning/schema/formats/989 references PASS, 12 tooling regressions PASS, whitespace PASS. Final outcome metadata checks recorded in STATUS.md. Hosted CI is NOT VERIFIED: build workflow reports active, but GitHub returned no run for ca7d475 at final inspection. No hosted result is inferred from local checks; manual release workflow and its 1.1.6 publication guard remain unchanged.

Real NIC/UAC/apply/reconnect/recovery, Narrator/high contrast/physical DPI, ordinary installed-app startup and owner acceptance are NOT RUN / UNVERIFIED. Other confirmations keep existing implementations. No phase completion/Phase 08 or GitHub Release authorized. Next safe step: close the old app using tray Exit and review local 1.1.7 using synthetic data.
