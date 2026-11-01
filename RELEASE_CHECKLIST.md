# Release checklist — release approval pending

ADR-045: owner explicitly requested publishing fork v1.1.1 with the new logo/current changes. Publish as Preview while the unchecked native/phase acceptance gates below remain visible; no claim that those gates are passed. Exact source/artifacts must pass the release workflow's unit/planning/package/runtime-isolation checks. First hosted run 41272961614 passed but predates the logo; repeat at the branded release source.

- [ ] All required phase acceptance gates complete and signed off.
- [ ] Base MIT license and attribution included.
- [ ] No unlicensed AV Toolbag source/assets introduced.
- [ ] `dotnet test` and Windows build green at release commit; commands logged.
- [ ] JSON schema versioning and legacy profile compatibility tested.
- [ ] Real network changes validated **only** with an isolated lab adapter.
- [ ] UAC, history/revert, Gateway/DNS, browser safety and corrupt JSON cases checked.
- [ ] Physical/USB/Hyper-V NIC selection tested.
- [ ] mDNS tests done; DHCP isolated test done if DHCP feature included.
- [ ] No secrets or real private device URLs in public release artifacts.
- [ ] Signed/unsigned binary, SmartScreen and update policy documented.
- [ ] ADR-039: primary installer and additional portable ZIP produced from the same application version/code, each bundling the required .NET runtime.
- [ ] Both artifacts verified on clean supported Windows without preinstalled .NET/SDK; no runtime-install prompt or external runtime download required.
- [ ] Installer install/launch/upgrade/uninstall checked with existing user data preserved; shortcuts/integration and scope documented. Portable full-folder extraction/launch checked separately.
- [ ] Both packages retain the same functional and UAC/network permission boundaries; no settings migration or USB-local storage claim without separate review.
- [ ] Changelog, known limitations and checksum attached.
- [ ] Release approved by user, not by autonomous agent.

## Audit-derived gates (not yet passed)

- [ ] Preparation 01-T06 through 01-T08 has evidence; exact safe unit command and excluded integration cases recorded.
- [ ] Full schema validation and semantic/round-trip checks passed; syntactic parsing is not counted as schema validation.
- [ ] New apply validates stable NIC identity after UAC, verifies actual state and handles partial failure/recovery without false success.
- [ ] Snapshot/support boundary covers affected settings or blocks unsupported complex configurations before mutation; old history limits documented.
- [ ] Project IO preserves originals on backup/replace failures; legacy remediation has its own reviewed scope.
- [ ] README test count/coverage and Windows support claims reconciled with actual release-commit evidence.
