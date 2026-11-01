# Release checklist — v1.1.2 Preview preparation; native acceptance pending

ADR-048: owner requested scoped commits/push of all current project changes and a new GitHub release. Prepare v1.1.2 Preview with both .NET 10 packages, all A26 fixes and the separate theme repair. Preserve v1.1.1 tag/assets; no force push, phase acceptance or native-network permission. Publication requires the exact new source to pass the existing release workflow. DOC-R01 intermittent diagnostics hide/cancel smoke failure remains disclosed, not silently suppressed. Record current source/CI/download hashes in STATUS; the checked results below belong to the historical 1.1.1 release until explicitly repeated for 1.1.2.

## Historical v1.1.1 evidence and remaining native gates

ADR-045: owner explicitly requested publishing fork v1.1.1 with the new logo/current changes. Publish as Preview while the unchecked native/phase acceptance gates below remain visible; no claim that those gates are passed. Exact source/artifacts must pass the release workflow's unit/planning/package/runtime-isolation checks. First hosted run 41272961614 passed but predates the logo; repeat at the branded release source.

Executed: [release workflow 41273489475](https://github.com/Elguajo/AV-Network-Toolkit/actions/runs/41273489475) PASS at `3fd7e77e1f5b560172b7e23d33928bace9cdaa73`; [v1.1.1 Preview](https://github.com/Elguajo/AV-Network-Toolkit/releases/tag/v1.1.1) published. 681 unit tests, 12 tooling tests and hosted package checks passed. Released downloads independently verified against manifest/SHA256SUMS and owner-approved EXE icon. Remaining unchecked items are not converted to PASS by publication.

- [ ] All required phase acceptance gates complete and signed off.
- [x] Base MIT license and attribution included.
- [ ] No unlicensed AV Toolbag source/assets introduced.
- [x] `dotnet test` and Windows build green at release commit; commands logged.
- [ ] JSON schema versioning and legacy profile compatibility tested.
- [ ] Real network changes validated **only** with an isolated lab adapter.
- [ ] UAC, history/revert, Gateway/DNS, browser safety and corrupt JSON cases checked.
- [ ] Physical/USB/Hyper-V NIC selection tested.
- [ ] mDNS tests done; DHCP isolated test done if DHCP feature included.
- [ ] No secrets or real private device URLs in public release artifacts.
- [x] Signed/unsigned binary, SmartScreen and update policy documented.
- [x] ADR-039: primary installer and additional portable ZIP produced from the same application version/code, each bundling the required .NET runtime.
- [x] Both packages verified on disposable Windows Server with global .NET discovery roots unavailable and a failing framework-dependent control; ordinary main-window startup, exact installed payload, locked update/uninstall rejection, same-version reinstall and uninstall fixture-data retention passed.
- [ ] Both artifacts verified on clean supported Windows without preinstalled .NET/SDK; no runtime-install prompt or external runtime download required.
- [ ] Installer install/launch/upgrade/uninstall checked with existing user data preserved; shortcuts/integration and scope documented. Portable full-folder extraction/launch checked separately.
- [ ] Both packages retain the same functional and UAC/network permission boundaries; no settings migration or USB-local storage claim without separate review.
- [x] Changelog, known limitations and checksum attached.
- [x] Release approved by user, not by autonomous agent (ADR-045 Preview scope).

## Audit-derived gates (not yet passed)

- [ ] Preparation 01-T06 through 01-T08 has evidence; exact safe unit command and excluded integration cases recorded.
- [ ] Full schema validation and semantic/round-trip checks passed; syntactic parsing is not counted as schema validation.
- [ ] New apply validates stable NIC identity after UAC, verifies actual state and handles partial failure/recovery without false success.
- [ ] Snapshot/support boundary covers affected settings or blocks unsupported complex configurations before mutation; old history limits documented.
- [ ] Project IO preserves originals on backup/replace failures; legacy remediation has its own reviewed scope.
- [ ] README test count/coverage and Windows support claims reconciled with actual release-commit evidence.
