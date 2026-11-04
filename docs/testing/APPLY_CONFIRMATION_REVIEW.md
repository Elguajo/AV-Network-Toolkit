# Compact local apply confirmation and refusal feedback

2026-11-04, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, clean baseline af4059e. UI-APPLY-01 / SWITCH-08 / ADR-060; Phase 07 AWAITING_REVIEW, unchanged. Implementation source is recorded in the outcome below after committing.

## Result and limits

Main local-profile/manual/hotkey/tray/history operations now show an adapter and four current/requested rows, with automatic/unchanged values translated for users. Technical details retain the full snapshot/routes/GUID/MAC/DAD behind an initially collapsed expander. Offline/conflict/address-only history warnings remain outside it. Reuse the actual ContentDialog host, typography/themes, native scrolling/fixed footer. Apply is explicit; Cancel receives initial focus and is the default. Enter on opening, Cancel and Escape refuse; focusing Apply and pressing Enter confirms. Existing project/Link-Local detailed reviews are retained.

Confirmed feedback defect: MainViewModel previously appended Apply.ManualRecovery to **every** non-complete result, including coordinator Rejected / MayHaveChanged=false before the journal or privileged worker. That produced the contradictory screenshot. ApplyOutcomeText now separates expiry, changed state/identity, busy, preflight, snapshot-write and UAC denial; unknown text remains generic/value-free. Recovery advice remains for MayHaveChanged=true. Project apply shares the precise no-change feedback while retaining its existing stage/snapshot/journal handling. No mutation policy/worker/elevation/schema/storage/dependency/version change, automatic retry or consent bypass.

**The owner's immediate refusal is not reproduced or claimed fixed.** A two-minute review timeout exists and is reproduced with a fake clock, but the owner explicitly reported immediate Yes. Current source has four coordinator Rejected stages: OperationActive, Expired, PreStateChanged and Preflight. The old main snackbar discarded that stage. Three passive reads using the source-extracted observer through Windows PowerShell 5.1, on the deliberately identified current Ethernet, were classified offline and identical across all snapshot fields. No raw snapshots/private addresses were retained in repository evidence. This rules out a difference during those samples, not the original failure or a latched earlier-worker/update barrier. No real commands, elevation, address/route/DNS writes, cable changes or probes were attempted. SWITCH-08 remains OPEN / FIELD_CAUSE_UNVERIFIED. Next observation: the specific message from this local candidate; investigate that stage without relaxing its safety checks.

## Verification

Private SDK `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`, private validation Python `$python = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`.

| Check | Result |
|---|---|
| `git archive --format=zip -o out/ApplyReviewBefore/source.zip af4059e`; expand, copy only current ProjectApplyViewModelTests; `test ... -c Release -p:OutputPath=bin/BeforeReview/ --filter FullyQualifiedName~ChangedPreStateNamesTheRefusalAndDoesNotSuggestPartialMutation --logger 'trx;LogFileName=before-refusal.trx' --results-directory TestResults/ApplyReview/Before` | Expected red: **1 FAIL**, missing precise changed-state feedback in prior source. Product unchanged in control |
| `test test/NetworkProfileManager.Tests.csproj -c Release --no-restore -p:OutputPath=bin/ApplyReview/ --filter 'FullyQualifiedName~ApplyConfirmationTests\|FullyQualifiedName~ProjectApplyViewModelTests\|FullyQualifiedName~LocalProfileApplyTests\|FullyQualifiedName~OfflineProfileApplyTests' --logger 'trx;LogFileName=focused.trx' --results-directory TestResults/ApplyReview` | **63 PASS**, before two additional automatic-DNS presentation cases |
| `test AVNetworkToolkit.sln -c Release --no-restore -p:OutputPath=bin/ApplyReview/ --logger 'trx;LogFileName=final-full.trx' --results-directory TestResults/ApplyReview` | **995 PASS / 0 failed/skipped**, 32 s; app/test build 0 warnings/errors |
| `build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj -c Release --no-restore -p:OutputPath=bin/ApplyReviewUi/` | PASS, 0 warnings/errors |
| Manifest-bearing `scripts/ProjectUiSmoke/bin/ApplyReviewUi/ProjectUiSmoke.exe TestResults/ApplyReview/FinalUi2 --apply-confirmation`, process-local DOTNET_ROOT set to private dotnet10 | **32 captures / 0 bindings**, Light/Dark 1200x850 and minimum 760x520, connected/static/offline/automatic-DNS/conflict, collapsed/expanded details, initial Cancel focus, opening Enter/Escape, automation Cancel and explicit Apply. Real main host/typed callback, fake network/journal/browser and isolated data root |
| Same EXE `TestResults/ApplyReview/FinalExisting`, default mode | **128 captures / 0 bindings**, existing project/device/apply/Link-Local/adapter/mDNS/workspace, target routing, modal/footer/focus/exit checks, final product source. Earlier Existing run also passed before localized polish |
| `$python scripts/validate_plan.py`; `$python -m unittest discover -s scripts -p test_validate_plan.py`; `git diff --check` | **1018 links / 12 tooling tests PASS**, whitespace PASS |

Unit additions cover exact reviewed request, cancellation/no journal or mutation before consent, two-minute/121-second expiry and a fresh explicit retry, DNS policy transitions/frozen server lists, unchanged/mask/gateway-removal summaries, visible warnings and no raw secret-bearing stages in failure messages. Partial/unknown worker paths retain recovery guidance. All network/process/browser/journal boundaries are fake in automated checks.

Retained preparation failures: initial smoke used RaiseEvent(Click), which does not execute the toolkit's template ICommand; corrected the **harness** to invoke the existing automation peer. A stricter initial-focus check then revealed DefaultButton alone does not set focus; **product** now requests last traversal target (Cancel) after template load and tests opening Enter without manually focusing it. Removed a nested scroll viewer after minimum-size visual inspection. One patch placed usings after the class (compile failed, corrected); the first tooling invocation used the venv root instead of Scripts/python.exe (app tests had passed, tooling had not run). No assertions suppressed or mutation checks weakened. Final tooling/package results follow below.

Native apply/UAC/IPC/OS execution/recovery, owner acceptance, Narrator/high contrast/physical DPI, original SWITCH-08 cause and later reachability remain **NOT RUN / UNVERIFIED**. Existing SWITCH-04–07 and phase gates remain. No running download/installation/user profile/project changes, push or GitHub Release.

## Committed source and local candidate

Exact implementation/package source **bd6c2f39a386e81170f644a230e9638936f16ad0**, committed locally on main; no push. Outcome-only evidence commit does not change that source. First commit attempt failed because no Git identity was configured; repeated with command-local name/email matching all five inspected recent commits. No global/local Git identity configuration changed.

Frozen export: `git archive --format=zip -o out/ApplyConfirmation-bd6c2f3/source.zip bd6c2f39a386e81170f644a230e9638936f16ad0`, expanded into its Source folder. Publish:

```powershell
& $sdk publish out/ApplyConfirmation-bd6c2f3/Source/app/NetworkProfileManager.csproj -c Release -r win-x64 --self-contained true -p:DebugType=none -p:DebugSymbols=false -p:PublishReadyToRun=false -p:SourceRevisionId=bd6c2f39a386e81170f644a230e9638936f16ad0 -p:InformationalVersion=1.1.7-local.apply-confirmation -p:ReleaseChannel=Preview -o D:/DEV/NETWORKING/AV-Network-Toolkit/out/ApplyConfirmation-bd6c2f3/Portable
```

PASS, MIT LICENSE from archive and existing third-party/native dependencies retained. Ready EXE: `out/ApplyConfirmation-bd6c2f3/Portable/NetworkProfileManager.exe`; keep all adjacent files. ZIP: `out/ApplyConfirmation-bd6c2f3/AVNetworkToolkit-local-apply-confirmation-bd6c2f3-portable.zip`. Extracted separately into ZipVerified and **all 8 file names/counts/SHA-256 match**.

Hidden `Start-Process -FilePath <candidate-exe> -ArgumentList @('--package-self-test', <fresh-absolute-json-path>) -WindowStyle Hidden -Wait -PassThru` exited 0 before ordinary startup. JSON Success/JsonValidated/WpfRendered true, assembly 1.1.7.0, informational `1.1.7-local.apply-confirmation+bd6c2f39a386e81170f644a230e9638936f16ad0`, .NET 10.0.14, Preview, no production key. Ignored package-self-test.json/package-manifest.json retain diagnostics/hashes without private network/user data.

EXE SHA-256 `d1cb83568f5f6107cf8947c8767ee4e1de18797127abd6f3bdf72ec533495e4a`; ZIP `071a80c0396d3a6aacc2f6aba27187c731bb89e30a3ca28696969079ffcbea1e`. No ordinary launch, installed/downloaded-binary replacement, private-data migration or native apply. Owner exits the old instance using tray Exit, then may review this candidate and report its specific refusal. **SWITCH-08 remains unresolved; this is a compact UI and corrected feedback candidate.**
