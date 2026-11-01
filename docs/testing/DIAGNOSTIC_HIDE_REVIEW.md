# Diagnostic hide/cancellation investigation — DOC-R01

2026-11-01, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, inspected base `386aef8f3a9d5d9345213a3a7c6eca77ca74a2ed` (full SHA in [retained evidence](evidence/diagnostic-hide-2026-11-01.json)). Owner selected investigation item 1 and will perform manual acceptance. Phase 07 remains AWAITING_REVIEW. Local scoped commit only; no push/release or native-network execution.

## Reproduction and cause

The unchanged `--phase07` reproduction first passed with 64 captures. A focused instrumented reproduction using the same real PingWindow, view model, fake platform and 10 ms injected interval then failed on its first iteration:

```text
iteration=0 beforeHide=2 atHidden=3 afterHide=3 afterRun=3 canStop=False
Hidden diagnostic window continued sending.
```

The assertion sampled `fake.Sends` before calling `Window.Hide()`. That sample is not the point at which the window becomes hidden and its IsVisibleChanged handler cancels the operation. Pending probe work was counted between that sample and the visibility handler: a further fake echo was counted by the time the visibility event was observed, and no additional echo was counted afterward. The final operation was stopped. The inline check also assumed that waiting for `AttemptCount >= 1` would mean exactly one send, although the shared `Until` helper pumps the dispatcher again before returning.

This is a smoke-test synchronization defect. The evidence does not establish a product cancellation defect. Existing MainWindow/PingWindow handlers and diagnostic services/view model remain unchanged. Native cancellation remains cooperative: an already running native echo can complete after cancellation; the suite does not claim packets already in flight are recalled.

## Correction and sensitivity

- An injected delay gate pauses continuous Ping after exactly one completed fake echo. The real window is then hidden; the check requires cancellation of that exact token, operation completion, restored selection availability, unchanged send count and no restart after Show.
- Both existing main/separate-window hide checks use the same gate, preserving their cancellation assertions without relying on scheduler speed or increasing a timeout.
- `--diagnostic-hide` exercises 10 Hide/Show cycles per real window and a separately gated in-flight echo per window. After Hide, the late echo is released; its result must be Cancelled, with no additional send or restart on Show. Storage and all network boundaries are isolated/fake.
- Two negative controls were built from a separate `git archive HEAD` export with only the updated harness overlaid. Removing PingWindow's actual hide-cancellation statement fails at `Hiding diagnostic owner did not cancel its active delay`. Restoring it and removing only MainWindow's corresponding statement passes the separate-window cases and fails on main with the same assertion. Both controls exit 1. Working-tree product sources were never modified.

## Commands and observed results

Private tools: `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet10/dotnet.exe"`, `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. SDK 10.0.403, Windows 11 host. All generated results/negative-control source copies are ignored under `TestResults/DocR01`.

| Command | Result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release -p:OutputPath=bin/DocR01/ --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/DocR01/ --nologo` | PASS, 0 warnings / 0 errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build -p:OutputPath=bin/DocR01/ --logger 'trx;LogFileName=doc-r01.trx' --results-directory TestResults/DocR01/Unit --nologo` | 723 PASS / 0 failed / 0 skipped |
| `& $sdk scripts/ProjectUiSmoke/bin/DocR01/ProjectUiSmoke.dll TestResults/DocR01/Final --phase07` | 64 captures, Light/Dark and normal/minimum, 0 binding errors; both gated hide checks PASS in all four combinations |
| `& $sdk scripts/ProjectUiSmoke/bin/DocR01/ProjectUiSmoke.dll TestResults/DocR01/FocusedFinal --diagnostic-hide` | 20 gated Hide/Show cycles and 2 in-flight echo cases PASS, 0 binding errors |
| `& $sdk scripts/ProjectUiSmoke/bin/DocR01/ProjectUiSmoke.dll TestResults/DocR01/Workspace` | 128 existing workspace captures and functional assertions PASS, 0 binding errors |
| Exported negative-control harness, `--diagnostic-hide`, each actual hide handler removed separately | Both intentionally FAIL exit 1 at the cancellation assertion |
| `& $py -B scripts/validate_plan.py`; `& $py -B scripts/test_validate_plan.py`; `git diff --check` | Final results recorded in STATUS handoff |

Initial invocation accidentally used the older private `dotnet` directory, which contains only SDK 8.0.427; it failed SDK selection before compilation. Switched to the already installed documented `dotnet10` directory; no SDK installation, global PATH change or global.json relaxation. Published application binaries, installer/ZIP and release remain unchanged.

## Disposition and manual check

DOC-R01 is RESOLVED as a harness defect, superseding historical OPEN/root-cause-unknown notes for this issue. This does not close UX-R01/P05-R01 or any native/owner/phase acceptance gate. No new requirement, public API, architecture, schema, storage location, UI design or production dependency changed. Focused diff review found no additional actionable defect.

The owner can check Continuous Ping in the inline and separate windows: start explicitly with the intended adapter/source, hide to the tray or hide/close the owning window, then reopen and verify the run is stopped and does not restart automatically. An already launched echo can finish after Stop. This is a suggested manual case, not authorization for the agent to send traffic or modify network settings; native packet/driver/UAC/IPC/route behavior remains NOT RUN by this task. Minimizing is distinct from Hide; this change adds no new minimize behavior.
