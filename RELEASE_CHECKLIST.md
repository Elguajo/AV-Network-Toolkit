# Release checklist — nothing approved yet

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
- [ ] Installer/portable artifact verified on clean Windows machine.
- [ ] Changelog, known limitations and checksum attached.
- [ ] Release approved by user, not by autonomous agent.

## Audit-derived gates (not yet passed)

- [ ] Preparation 01-T06 through 01-T08 has evidence; exact safe unit command and excluded integration cases recorded.
- [ ] Full schema validation and semantic/round-trip checks passed; syntactic parsing is not counted as schema validation.
- [ ] New apply validates stable NIC identity after UAC, verifies actual state and handles partial failure/recovery without false success.
- [ ] Snapshot/support boundary covers affected settings or blocks unsupported complex configurations before mutation; old history limits documented.
- [ ] Project IO preserves originals on backup/replace failures; legacy remediation has its own reviewed scope.
- [ ] README test count/coverage and Windows support claims reconciled with actual release-commit evidence.
