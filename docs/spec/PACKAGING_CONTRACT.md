# REL-03 packaging and safe verification contract

2026-11-01 Asia/Tashkent. ADR-039 delivery choice and ADR-040 execution. Phase 07 remains AWAITING_REVIEW. Current application version is retained; final release version/changelog/README/checksum publication belongs to REL-04. No tag/release or live-network permission here.

## Scope and approach

One self-contained win-x64 publish produces both the primary Inno Setup installer and additional ZIP. Keep executable/assembly/data identities, embedded schema, JSON and all network/UAC behavior. Installer goes to Program Files by default, requires administrator permission for installation, adds a Start menu shortcut and a registered uninstaller. It never auto-launches the app, creates a service/startup entry, applies settings, registers file associations or downloads .NET. It never deletes LocalAppData or user-chosen projects. Existing UI is unchanged.

The current supported build target is .NET 8/WPF. Inno Setup **6.7.4** is a build-only dependency, pinned to immutable official release `is-6_7_4`; installer asset SHA-256 `A549929319F9120716677C685535CA4671E4B937B5FDC87E2585F32D8EB30E8C` verified against its GitHub release digest. Compiler setup runs per-current-user under the existing build-tools directory, without elevated product installation on this PC. Its license permits distribution and requires retaining original notices; no modified engine or unlicensed AV Toolbag code/assets.

Pin configuration is `packaging/inno-setup.json`, including installed ISCC/ISCmplr/ISPP hashes derived from the verified official installer. Verify these bytes on reuse or an explicitly supplied compiler; reject missing/incorrect/tampered tools. Do not trust PE FileVersion: the official compiler binaries report 0.0.0.0. Candidate manifests identify the pinned release/files rather than inventing a PE version. `test_packaging_tools.ps1` covers reuse plus four negative cases without changing the real build tool.

Payload includes the base MIT LICENSE, existing dependency notices and all five WPF native DLLs beside the EXE. Retain existing no-native-extraction policy; no trimming/AOT or dependency upgrade. Compiler/package generation is a separate script with explicit failure checks. Refuse output reuse and dirty source by default; a verified git archive can pass its exact source SHA. The manifest records source commit/version, compiler version, self-contained target and SHA-256 of candidate packages as build evidence, not release publication. Installer and ZIP must have identical payload bytes.

Installation/reinstallation/uninstall must not force-close a busy app, restart it or request a reboot. PrepareToInstall and InitializeUninstall test exclusive writable access to an existing target EXE and block when unavailable, so the user can finish the active operation and exit deliberately. Hosted tests hold a read lock and require both update/uninstall rejection. This cannot certify every OS/process race. Cross-version upgrade and actual interactive UAC behavior require separate evidence; same-version reinstall is labelled accurately.

## Explicit packaged diagnostic

`NetworkProfileManager.exe --package-self-test ABSOLUTE_NEW_REPORT_PATH` is an explicit non-product diagnostic. It routes before normal App startup: no migration, LocalAppData writes, settings/theme/startup registration, main/tray/network services, NIC enumeration, probes, worker/elevation or browser. It loads existing App/WPF-UI resources, validates a harmless embedded-schema project and renders an existing-style Card offscreen to force WPF/native dependencies. It writes version/runtime/result JSON with create-new semantics and exits. Invalid arguments/relative path/existing report fail with code 2; validation/render failure is code 1 and only the exception type is reported. It never overwrites an existing report or creates its parent directory.

Successful self-test proves the packaged host/runtime/managed resources/JSON/WPF rendering can load. It does **not** certify ordinary App startup, tray, privileged IPC, actual network configuration, user workflows or a full manual Windows walkthrough.

## CI and isolation

Keep the existing main/PR build-and-unit job, add explicit workflow_dispatch for diagnosable execution, retain TRX evidence and run the existing planning/schema/tooling checks. A dependent windows-2022 job builds both packages. Contents permission is read-only; no secrets, signing, releases or network probes. Upload only candidate packages/manifest and safe reports as Actions artifacts.

`test_packages_on_runner.ps1` is guarded **before any mutation** to require GITHUB_ACTIONS=true, RUNNER_ENVIRONMENT=github-hosted and Windows ImageOS. Local invocation must reject. It builds/runs a framework-dependent .NET 8 control while the runtime is available; shuts down build servers, verifies allowed runtime-root paths, moves those roots aside temporarily, clears process DOTNET_ROOT variables and requires the same control to fail with a runtime/hostfxr error. Only then does it test extracted ZIP and installed payload, exact payload hashes, self-test positive/negative cases, same-version reinstall, uninstall and retained fixture user data. Restore moved runtime roots/environment in finally. No deletion/move of arbitrary computed paths, no OS/network changes on the user's computer.

The hosted script also launches each package normally, requires an idle process with the expected main window handle/title, then terminates only its own smoke process without clicking Run/Apply/Open. Ordinary startup reads local adapter facts and uses disposable runner user storage; it never changes NICs or starts probes. This normal-startup check is not run on the user's computer.

This is a disposable hosted Windows **runtime-discovery isolation test**, not a pristine Windows client image or a real NIC acceptance claim. True cross-version upgrade, actual UAC consent/denial and network recovery remain NOT RUN unless separately observed; main-window smoke is not full manual UI/tray acceptance. Removing only PATH/DOTNET_ROOT is insufficient evidence; the framework-dependent negative control is mandatory. Hosted execution remains pending until the concrete local commit is authorized for push and the run actually completes.

## Authoritative references consulted

- [Microsoft .NET publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/): self-contained distribution and separate runtime discovery.
- [Inno Setup immutable 6.7.4 release](https://github.com/jrsoftware/issrc/releases/tag/is-6_7_4), [license](https://github.com/jrsoftware/issrc/blob/is-6_7_4/license.txt), [setup parameters](https://jrsoftware.org/ishelp/topic_setupcmdline.htm), [privileges](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm), [close behavior](https://jrsoftware.org/ishelp/topic_setup_closeapplications.htm), [architectures](https://jrsoftware.org/ishelp/topic_setup_architecturesallowed.htm).
- [GitHub workflow enable/disable](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/disable-and-enable-workflows): inspect configuration and actual runs rather than assume a fork is disabled. Observed Actions enabled=true, both existing workflows active, runs=0; historical absence cause remains UNVERIFIED until a discriminating execution is available.

## Storage steering — ADR-042

Owner selected future `%USERPROFILE%\.av-network-toolkit` **after packaging**. Current packages preserve existing LocalAppData. Follow with a separately reviewed data migration/backup/conflict/recovery change; this contract does not silently move customer files or advertise USB-local settings.
