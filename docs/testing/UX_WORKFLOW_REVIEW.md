# Main workflow usability review — 2026-11-01

Base main `ef2e4773a36fa2d7feb941655e07427fcf7ae108`. Owner explicitly requested a WPF scenario audit followed by local UX fixes under the approved second concept. This is expert observation of actual controls with fake external boundaries, not user research or native network acceptance.

## Before implementation: observed findings

Executed `ProjectUiSmoke.dll TestResults/UxBefore --ux-before` against the baseline application components. Real MainWindow, ProjectProfileDialog and ProjectApplyWindow were shown/rendered; typed in the IP TextBox, invoked Keep, closed via Window.Close (same closing path as ×), changed adapter selection and read fixed-target state. All fixture IO stays under ignored TestResults; no App startup/NIC APIs/UAC/browser.

| ID | Severity / confidence | Problem and observed evidence | Minimal action |
|---|---|---|---|
| UX-01 | High / high | Dirty modal closes without any discard prompt when dialog consent is false; observations.txt records 0 prompts. User can lose work through × | Guard modal closing with existing discard confirmation; do not prompt twice on approved application exit |
| UX-02 | High / high | Invalid computer IP shows “Project does not match the supported schema…” and `($)`; the bad field is below the viewport at 480px | Field-specific draft checks, inline error and focus/scroll to first error; retain authoritative full-project validation |
| UX-03 | Medium / high | Locked target ComboBox is disabled, but status asks user to choose an adapter and claims none is selected automatically | Name the fixed target and point to Review; keep GUID binding/policy unchanged |
| UX-04 | Medium / high | Main/project toolbar exposes only Save as; modal Keep adds only to in-memory project. Screenshot has “Not saved / Unsaved changes” but no normal Save | Explicit Save project file versus Save as, reuse exact conflict/backup/confirmation IO and show feedback |
| UX-05 | Medium / high | Main empty selection displays a single instruction, profile source distinction is mainly tab text, and errors are at bottom of outer scroll | Explain local/project storage, add visible refresh/prepare-project actions, pin actionable feedback |
| UX-06 | Medium / high | Apply status resides inside scrolling content; review/recovery text competes with the immediate next step at minimum size | Pin status with confirmation/cancel actions; explain denial/partial outcome without asserting success |

Baseline captures: ignored `TestResults/UxBefore/main-project.png`, `editor-invalid-ip.png`, `main-no-adapter.png`, `apply-fixed-target.png`; `observations.txt` records reproduced error/close/status. Baseline focused run PASS / 0 binding errors means reproduction succeeded, not that UX was acceptable.

Platform reference: [Microsoft validated forms](https://learn.microsoft.com/en-us/windows/apps/get-started/line-of-business/build-validated-form) recommends textual field errors, preserving input and moving focus when failed submission involves an offscreen field. This is interaction guidance only; the implementation remains WPF and adds no WinUI dependency.

## Implemented dispositions

- UX-01: modal Cancel, × and application exit use the existing deliberate discard confirmation. Declining keeps the detached draft, including invalid input; keeping successfully does not prompt to discard.
- UX-02: the modal identifies name/address/prefix/gateway/DNS errors, brings the first invalid field into view, focuses/selects its content and updates feedback when corrected. Computer IP is labelled explicitly. Optional alias/ID move into an existing Expander. Presentation checks do not replace ProjectJson validation or alter /31–/32 apply restrictions. Import/full-document and the manager's older inline form can still return document-level errors.
- UX-03: fixed target label/status names the main-screen NIC and points to Review settings; manual-target callers retain deliberate selection. Refresh/prepare show reading state. Review is accented before a review exists; confirmation becomes the primary enabled action after review. No target fallback, coordinator/worker/policy change or inferred connectivity.
- UX-04: Save project file writes an opened project to its current path; first Save and Save as use the path dialog. Existing overwrite confirmation, original revision comparison, safe write and backup are reused unchanged. Keep in project versus Save file versus Apply have explicit copy. Feedback clears when editing resumes; cancellation/failure retains unsaved state.
- UX-05: local storage hint names selected-adapter association and the five-slot limit; project profiles remain portable and independent. No-adapter state gives Refresh and Open/prepare project. Disconnected link no longer replaces the configured computer IP with “Not connected”. Project errors/open/save messages stay in the main footer while content scrolls.
- UX-06: apply status and Review recovery remain beside the footer actions at minimum size. Empty recovery explains why nothing can be restored; denial and partial failure retain truthful state/stage/observations without browser launch or automatic recovery.

On 2026-11-01, the owner explicitly called the circled tabs convenient: Network profiles / Devices, On this computer / Project profiles. Preserve this hierarchy. This is limited owner feedback on tabs, not acceptance of the entire journey or Phase 06. Save/apply distinction and recovery discoverability still need a task walkthrough.

## Actual WPF evidence

These are live-control RenderTargetBitmap captures with fake data, composited over the existing theme surface because native Mica is outside this render API. They are not generated mockups or proof of native desktop/assistive-technology behavior. Main fixtures use documentation IP ranges; no user network or credentials are captured.

| Capture | Purpose |
|---|---|
| [Main, dark 1200×850](../design/ux/main-project-dark.png) | Adapter → observed facts → project → scoped rows → target-named Apply |
| [Main, light 760×520](../design/ux/main-project-minimum-light.png) | Scrolled to rows; long summaries wrap, per-row actions remain reachable |
| [Saved project](../design/ux/project-saved.png) | Ordinary save, same path, explicit non-network result |
| [Editor error](../design/ux/editor-error.png) | Invalid IP focus/error/kept input at 520×480 |
| [Partial failure](../design/ux/apply-partial-failure.png) | Visible stage, recovery action and truthful uncertainty at 620×520 |
| [No adapter](../design/ux/no-adapter-minimum.png) | Empty adapter enumeration, next actions at 760×520 |
| [Before: editor](../design/ux/before-editor-error.png), [before: target](../design/ux/before-fixed-target.png) | Baseline generic error and contradictory selection guidance |

Final artifacts: ignored `TestResults/UxAfter` (12 PNGs + observations + safe fixture JSON), `TestResults/UxFull` (112 PNGs), `TestResults/UxUnit/ux-final-failed.trx`. Reusable safe control driver: `scripts/ProjectUiSmoke/UxWalkthrough.cs`.

## Commands and results

Windows / PowerShell, .NET SDK 8.0.427. Set `$sdk = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/dotnet/dotnet.exe"`; `$py = "$env:LOCALAPPDATA/AVNetworkToolkit.BuildTools/python/Scripts/python.exe"`. Paths below are relative to repository root. No actual App startup, NIC enumeration/change, probe, scan, socket, administrator process, DHCP server, native dialog or browser.

| Exact command | Final result |
|---|---|
| `& $sdk build AVNetworkToolkit.sln --configuration Release --no-restore -p:OutputPath=bin/UxUnit/` | PASS / 0 warnings, 0 errors |
| `& $sdk test test/NetworkProfileManager.Tests.csproj --configuration Release --no-build -p:OutputPath=bin/UxUnit/ --logger "trx;LogFileName=ux-reviewed.trx" --results-directory TestResults/UxUnit` | **FAIL: 564 passed / 1 failed / 0 skipped, total 565**; see blocker below |
| `& $sdk build scripts/ProjectUiSmoke/ProjectUiSmoke.csproj --configuration Release --no-restore -p:OutputPath=bin/Ux/` | PASS / 0 warnings, 0 errors |
| `& $sdk scripts/ProjectUiSmoke/bin/Ux/ProjectUiSmoke.dll TestResults/UxAfter --ux-after` | PASS / 12 captures, 0 binding errors |
| `& $sdk scripts/ProjectUiSmoke/bin/Ux/ProjectUiSmoke.dll TestResults/UxFull` | PASS / 112 captures, 0 binding errors; Light/Dark and normal/minimum |

The focused driver invokes real WPF buttons/commands and form bindings: first and repeat Save (one path picker), actual Add → modal → Keep, invalid submission/focus/correction, declined close, empty-source Refresh, disconnected configured IP, pinned errors, fixed target, empty recovery, declined final confirmation, fake UAC denial, partial failure, loading/disabled actions. Full driver retains project/device selection-draft guards, per-row selected-GUID routing, browser-only Open, recovery, cancellation/close and forward keyboard reachability. Thirteen added unit cases cover normal save path/cancel/overwrite/conflict/IO failure and field-validation dependencies. They passed in the final suite; no existing JSON tests altered.

Manually inspected the stored baseline and final dark/light/minimum captures, field focus and partial-error footer. WPF-UI styles/resources and names/live settings are reused. Self-review of the scoped diff found no remaining actionable defect within these changes; this is not an independent review or full accessibility certification. No copied assets or new dependency.

Initial failures corrected: test fixture assigned an init-only property; simultaneous test/smoke compilation collided in shared obj (builds then sequenced); new driver referenced an unavailable wait helper (replaced with bounded dispatcher pump). Visual inspection caught a Review button style without its WPF-UI base, then fixed and re-rendered it. A stale validation footer after correction was fixed and covered. An earlier full unit invocation passed 565/565; it does **not** supersede the final failure. Its TRX was overwritten by the final invocation; the failed TRX has been separately preserved.

## Blocking regression evidence — UX-R01 / P05-R01 investigation

The final full run did not reject `ProjectJsonTests.RejectsInvalidStructureOrSemantics(variant: "dhcpIpv4", code: "Structure")`: Assert.Throws reported no exception at line 141. [Preserved failure excerpt and TRX hash](evidence/ux-json-failure.txt). This is another unsafe JSON-rejection observation in the same validator family as OPEN P05-R01 (previously emptyStaticDns). Whether they share a root cause, or whether suite timing from these additions exposes it, is **UNVERIFIED**. Do not label it a proven pre-existing variant or a proven UX regression.

ProjectJson.cs, ProjectStore.cs, schema, existing JSON tests, apply coordinator/policy/worker, NetworkService and legacy persistence are unchanged from ef2e477. No serialization, suppression, threshold change, schema repair or green retry here. Under AGENTS.md, out-of-phase bugs are logged for separate review. **Full regression gate FAIL; release NO-SHIP.** Local UI evidence does not close this issue or demonstrate reliable authoritative validation.

## Remaining acceptance and safe next step

- **Owner task validation pending:** without explanation, choose a NIC, describe which IP belongs to the computer, create a profile, distinguish Keep from file Save, locate the intended Apply and reach Review without confirming a real change. Then find device Open versus Apply & Open and recovery. Tabs alone have positive owner feedback; no measured task success/time/first-click claim.
- **Practical network support still blocks some real jobs:** DHCP without a usable lease/APIPA, complex/managed IPv4/DNS/routes, static automatic DNS and /31–/32 retain their conservative apply gates. IDEA-02 needs a separate support design; UI cannot make those transitions supported. The older inline manager can still show generic full-document errors.
- **Credentials remain IDEA-03:** owner permission ADR-024 is preserved; passwords are not implemented in current JSON v1. No silent format/security change in this UX work.
- **Native/manual NOT RUN:** full App/tray startup, native file/consent dialogs, real UAC/worker/browser/TLS, isolated NIC apply/recovery, screen reader, high contrast, Windows scaling/zoom and large catalogs. No release, push, phase acceptance or Phase 07 start.

Changed areas: main/project/profile/apply XAML and their view models, English resources, safe WPF driver and focused tests, plus this evidence/planning handoff. Phase 06 remains AWAITING_REVIEW; IDEA-04 has local evidence only. Next smallest safe work is separately scoped deterministic JSON validation investigation and owner UX walkthrough; no automatic network test or phase advancement.
