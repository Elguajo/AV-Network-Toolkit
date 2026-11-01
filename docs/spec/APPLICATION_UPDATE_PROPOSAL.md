# Application updates — proposal and implementation preflight

2026-11-02, Asia/Tashkent. **PARTIALLY_APPROVED / PORTABLE_DECISION_PENDING**, UPD-01/02, ADR-053. Owner selected the signed-manifest trust model in this chat; portable strategy remains pending before implementation. Inspected fork https://github.com/Elguajo/AV-Network-Toolkit, clean `main` at `05cac3bf054f282d8e54fc39e28424b0a9dd5754`. No keys, shared-resource changes, installation, push or publication authorized. Phase 07 remains AWAITING_REVIEW; DATA-01 and Phase 08 are separate.

## Observed system

| Boundary | Evidence and implication |
|---|---|
| Runtime/distribution | [Project](../../app/NetworkProfileManager.csproj) targets net10.0-windows, version 1.1.5, existing NuGet versions retained. [Build script](../../scripts/build_packages.ps1) publishes one self-contained win-x64 payload for installer and ZIP; five native WPF DLLs remain beside the EXE. |
| Installer | [Inno script](../../packaging/AVNetworkToolkit.iss), pinned 6.7.4, fixed AppId `{02E72C36-F6DD-4AB0-92EC-A66017AA227A}`, admin install, default Program Files. No automatic launch, reboot, service or startup registration. Exclusive EXE-open checks reject busy install/uninstall. This is not a transactional rollback guarantee. |
| Release integrity | [Workflow](../../.github/workflows/release.yml) publishes packages, unsigned package-manifest.json and SHA256SUMS.txt. Manifest has source/version/package hashes, not an independently authenticated publisher signature. |
| Current public release | Read-only GitHub connector checks on this date: release ID 430755509, v1.1.5, prerelease=true, draft=false, immutable=false. Target and independently read lightweight tag both identify `9e06c48b4b433e0463c2376ba4555c44804ea010`. Four assets; ZIP 68,371,066 bytes, installer 67,838,199 bytes, manifest 830 bytes, checksums 219 bytes. API digest values are available. No assets downloaded or executed in this investigation. [Previous package evidence](../testing/RELEASE_1.1.5_REUPLOAD_REVIEW.md) remains historical, not rerun. |
| Exit/tray | [App](../../app/App.xaml.cs) hides the main window on Close. Tray Exit checks Apply windows, project drafts and main-window confirmation before Shutdown; OnExit disposes monitor/tray. A new update action must share this guarded exit path. |
| In-flight work | [Main guard](../../app/MainWindow.xaml.cs), [local Apply guard](../../app/ViewModels/MainViewModel.cs), [Apply window](../../app/Views/ProjectApplyWindow.xaml.cs), [mutation lease](../../app/Services/NetworkMutationLease.cs). Busy guards request cancellation and refuse immediate exit; cancellation is not completion. [Activity tracking](../../app/MainWindow.Activity.cs) retains closed diagnostics until actual completion. |
| Data | [AppPaths](../../app/Helpers/AppPaths.cs): LocalAppData/NetworkProfileManager/data; [recovery journal](../../app/Services/ProjectApplyJournal.cs): sibling apply-recovery. Explicit project files may live anywhere. ZIP uses the same data roots, not USB-local storage. |
| Safe persistence pattern | [WindowLayoutStore](../../app/Services/WindowLayoutStore.cs) with [LegacyJsonFile](../../app/Services/LegacyJsonFile.cs) preserves corrupt/future data, backups and revision conflicts. Use this pattern for additive update preferences rather than expanding the best-effort settings writer. |

No Progressive Context Kit router was found among tracked agent/router paths. Repository AGENTS.md and its planning hierarchy apply; README FOR AGENTS.md contains historical onboarding statements superseded by current state.

## Recommended minimum

Keep the existing application and packages. Add a small testable update service/coordinator, an existing-style update view in Settings, and a shared guarded exit operation. Use built-in .NET HTTP, SHA-256, RSA and ZIP APIs; no new runtime dependency, service, scheduler or installer replacement is proposed.

1. **Check:** explicit Check for updates always available. Optional automatic checking is off until enabled; check after ordinary startup, at most once per 24 hours, without blocking startup. Never run checks from package self-test or elevation workers. No background download, install or app exit. Only public release requests; no tokens, project names, adapter facts, private paths or telemetry.
2. **Review:** show current/offered version, Preview label, package type, download size, release notes as plain text, source identity and compatibility limitations. Keep release links restricted to the fixed repository. State unavailable/offline/rate-limited/incomplete results separately from Up to date.
3. **Download:** explicit Download with progress and Cancel; an incomplete file cannot become installable. Stream into a unique private staging directory outside application/data/project roots. Verify declared byte length and hash, and recommended detached publisher signature described below. Never execute before verification.
4. **Install:** a second explicit Install update action explains app exit and installer UAC. Revalidate the frozen offered identity and staged bytes. A missing/changed package, invalid signature, ambiguous install target or incompatible platform blocks execution with manual-release guidance.
5. **Preserve:** no profile/project/history/recovery/layout migration or deletion. A private versioned update preference may hold opt-in, channel and last successful check. Unreadable/future preferences are preserved and automatic checks default off. Downloads and their cleanup never traverse data/project directories.

### Version/channel policy

- Preview builds offer published Preview or stable versions; stable builds offer only stable unless the user explicitly enables Preview. Persist that choice; never infer it from a tag suffix or silently switch it. New packages need explicit build-channel metadata; today's three-part version does not encode Preview.
- List releases, not `/releases/latest`, which excludes prereleases. Parse strict supported numeric versions; exclude drafts and incompatible RID/platform/data contracts. Compare versions numerically, not by date or lexical order. Select the highest verified compatible candidate, never an automatic downgrade.
- Pin release ID, asset IDs, source commit, package length/hash and channel for the entire operation. GitHub metadata is discovery input, not authority to override a signed manifest. Handle pagination with a finite budget and an explicit incomplete-result state; never report Up to date from a truncated list.
- For v1, require a higher application version. Same-version re-uploads such as the original/corrected 1.1.5 remain a deliberate manual repair, not an automatic reinstall. Future automatic update releases should use a new version for changed bytes. Existing tags/assets are untouched.
- The current 1.1.5 has no updater. One normal manual installation/extraction of the first updater-enabled version is required. Unsigned older releases remain available manually; signature failure never triggers an unsigned automatic-install fallback.
- Retain the documented .NET 10 Windows edition restrictions. A bundled runtime does not expand OS support. Unknown support/data compatibility blocks installation pending manual review.

### Source and authenticity — owner selected signed manifest

**Owner-selected trust model:** add a separate versioned `update-manifest.json` plus detached signature for each future update release, retaining the current package-manifest contract. Embed only the trusted public key in the first updater-enabled build. Verify exact bounded UTF-8 manifest bytes with RSA-3072 / SHA-256 / PSS before interpreting trusted fields; reject duplicate fields, unsupported schema/algorithm/key and malformed values. The manifest binds product/repository, version/channel, exact source commit, RID, supported OS/data compatibility, package basename/type, byte length and SHA-256. Names cannot contain paths. Verify package bytes against these signed values immediately before use; API digest is an additional consistency check.

For portable extraction the signed manifest also binds the expected relative payload filenames, sizes and hashes; these define the complete allowed file set. They are additional update metadata, not a change to the existing unsigned package-manifest schema.

The real private key stays outside the repository and application. Key generation/storage, signing custody/recovery and any CI secrets/environment changes require a separate concrete owner-approved operational step before release enablement. Local tests use disposable test keys only. Never ship a test key as a production trust root. Loss/rotation requires a separately reviewed bootstrap/transition; the client must not trust a new key merely because it was downloaded. Authenticode is a separate future choice; a signed manifest does not remove SmartScreen/UAC publisher warnings from unsigned EXEs.

**Alternative not selected:** fixed GitHub HTTPS origin plus API SHA-256 and package manifest, trusting GitHub/account integrity. This detects transport corruption but does not authenticate against an attacker who can replace both package and metadata. There is no unsigned fallback. Neither model guarantees discovery freshness against withholding/replay; prevent downgrade, keep last verified identity and do not claim a complete update-security framework.

All redirects require HTTPS and exact allowed GitHub download hosts established from official behavior during implementation. No arbitrary host, credentials, TLS bypass, shell commands or user-provided download URLs. Bound metadata, package size, redirects, request time and retries; honor rate-limit reset and cancellation. Guard staging paths/reparse points and package replacement between verification and launch. Verification-to-elevated-execution behavior must be demonstrated in the isolated lab before enabling installation, not assumed from a hash check.

### Installed application

Detect installed mode only when the fixed AppId's Windows uninstall entry and canonical running EXE directory agree. Do not classify by folder name or treat any Program Files EXE as installed. A portable copy running beside an installed version stays portable. Ambiguous/custom/unavailable registry evidence offers explicit package download/manual instructions rather than a guessed destination.

Reuse the interactive installer, stable AppId and discovered existing destination; pass only fixed documented parameters such as `/NORESTART`, not silent/force-close flags. No automatic relaunch in v1, preserving packaging behavior. Explain that the user starts the app again after the wizard finishes; launching the wizard is not reported as successful installation.

Before launch, obtain the shared exit gate: resolve drafts through existing dialogs, block new local/project/tray/hotkey mutations, request cancellation of owned diagnostics/scanner/mDNS, wait for actual completion and durable recovery results. Timeout or refusal aborts the handoff; no process kill or new cancellation semantics for network mutations. A cancelled exit restores normal operation. Existing one-way NetworkMutationLease.Inhibit is for uncertain mutation state and cannot be reused as a reversible update lock.

After guards succeed, launch the verified interactive installer and then complete real app exit, including tray disposal. On UAC denial/launch failure, keep the app available and the verified download retryable. Retain installer busy-file rejection if another instance or an exit race keeps files occupied. A helper is not required for this proposed interactive first version; reliable waiting/silent install/relaunch would be additional scope. Validate launch/exit ordering in isolation. Do not advertise atomic installer rollback or guaranteed cross-version recovery.

### Portable — owner choice pending

**Recommended minimum:** after explicit destination selection, verify and extract into a newly created versioned folder. Reject current/application/data/project directories, existing nonempty targets, traversal, absolute/ADS/device paths, symlinks/reparse entries, duplicate or colliding names, and excessive entry count/expanded size. Extract only package-declared files. Publish the ready folder only after all files verify; failure leaves the current version usable. Display the exact new EXE location. After protected Exit, the user starts that EXE; do not copy settings into it or create an installed-app identity. Existing startup registration only follows the app's current enabled-path refresh behavior when the new copy is deliberately started; disclose the path change.

**Alternative:** preserve the same portable directory using a dedicated out-of-process updater, verified handoff, process-identity wait, staged file-set replacement, backup and crash-recovery journal. Never use a downloaded script or ad hoc delayed shell copy. This has a larger compatibility/recovery surface and needs its own implementation contract. Do not silently reduce a same-directory requirement to a ZIP download.

## Implementation sequence after decisions

1. Record owner decisions and replace this proposal with a bounded approved contract; retain nine phase IDs and phase state. Define trusted-manifest schema, update-state persistence and package/channel metadata. Signing operations remain a separate gate.
2. Add pure release/version/channel policy and injectable HTTP/downloader/verifier/filesystem/process boundaries. Add cancellation/progress and a single update coordinator; keep network-configuration services unchanged.
3. Add Settings UI/English resources using DESIGN.md; share the existing tray Exit protection with the update action and add a reversible launch barrier/drain checks. No broad UI rewrite.
4. Implement installed handoff and the selected portable policy. Update package generation and local signing tooling only within the approved scope; preserve old manifest/installer/data behavior.
5. Run focused negative tests, full isolated unit/build checks, WPF fixtures and documentation validation. Produce an evidence packet and scoped commits on main. Public release integration, real keys, secrets, push and disposable installation/UAC execution are separately authorized actions.

## Acceptance evidence required before calling it implemented

| Area | Required checks |
|---|---|
| Discovery | Stable/Preview opt-in, numeric ordering, malformed tags, drafts, incompatible OS/RID/data, same-version/downgrade exclusion, pagination/incomplete results, offline/timeout/403/429/304 and no automatic traffic when disabled. Fake HTTP only. |
| Trust/download | Valid/test-wrong-key/tampered/missing signature, duplicate manifest fields, changed source/assets during download, missing digest policy, truncated/oversize/content-length mismatch, partial files, cancel/retry, untrusted redirects and post-verification replacement. No real production key. |
| Lifecycle | Dirty project/profile dialogs, busy local/project Apply, late cancellation, diagnostics/scanner/mDNS drain, new apply from tray/hotkey during handoff, two update clicks, other app instance, UAC denial/launch failure, real exit versus hide-to-tray. Fixture process launch, no installer on this PC. |
| Storage/portable | Corrupt/future/conflicting update preferences; data/project/recovery bytes preserved; unknown install mode; ZIP traversal/ADS/reparse/case collisions/bomb limits, read-only/offline/removable disk, disk full, partial extraction and restart after failure. Temporary fixtures only. |
| Regression/UI | Existing profile/scanner/history/revert/UAC tests; Release build; focused and full isolated unit suite; Light/Dark normal/minimum WPF captures, keyboard/errors/progress/cancel and supported English. No live NIC/traffic. |
| Native/release gate | Separately approved isolated supported Windows: cross-source old-to-new upgrade, denied/accepted UAC, busy second process, installer failure, unchanged fixture user data and actual tray exit. Verify both same-source package payloads. Published signature/source/download checks only after publication authorization. |

Investigation performed source inspection and read-only public metadata/documentation requests only. Application build/unit/native/install checks are **NOT RUN** for this proposal. Earlier 744-unit/package evidence belongs to 9e06c48 and is not evidence for an updater.

## Primary documentation consulted

Context7 resolved `/jrsoftware/issrc`, `/websites/github_en_rest` and `/dotnet/docs`; queries covered stable AppId/installer behavior, release listing/Preview/assets and .NET signature verification. Official pages checked on 2026-11-02:

- [GitHub releases API](https://docs.github.com/en/rest/releases/releases), [asset API](https://docs.github.com/en/rest/releases/assets), [rate limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api). Public release discovery needs no app token; `/latest` excludes prereleases.
- [Inno AppId](https://jrsoftware.org/ishelp/topic_setup_appid.htm), [CloseApplications](https://jrsoftware.org/ishelp/topic_setup_closeapplications.htm), [setup command line](https://jrsoftware.org/ishelp/topic_setupcmdline.htm). Reuse the pinned installer; never force-close or silently reboot.
- [.NET RSA.VerifyData](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsa.verifydata?view=net-10.0), [RSA.ImportFromPem](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rsa.importfrompem?view=net-10.0). Built-in public-key verification needs no new package.
- [GitHub immutable releases](https://docs.github.com/en/code-security/concepts/supply-chain-security/immutable-releases): potential future release hardening, not enabled or required here. Current release remains mutable; no repository setting change authorized.
