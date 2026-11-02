# Device directory and modal editor — UI-DEVICE-01

2026-11-02, Asia/Tashkent. Owner approved both raster concepts with «Ну ок делаем тогда также», after confirming that the list supports many devices and the right pane shows one selected device. Local implementation on main; Phase 07 remains AWAITING_REVIEW. No installation, live network/browser/UAC, publication or Phase 08 authorization.

## Approved visual intent

- [Add-device raster](../design/devices/add-device-target.png): native modal, required name/full URL first, optional profile/password/details disclosures, fixed Cancel/Add footer and explicit project-draft/file-save distinction.
- [Selected-device raster](../design/devices/device-details-target.png): many-device navigation at left; read-only selected name/URL/status at right; explicit Open/Edit, linked Apply & Open and project-save feedback.
- Original imagegen concepts produced in this chat; no AV Network Toolbag assets. Existing [DESIGN.md](../../DESIGN.md), WPF-UI controls, semantic resources and native focus behavior remain authoritative. Retain actual rail, adapter context, project bar and Project manager. Mock backgrounds are illustrative, not a full-window redesign or pixel-identity requirement.

## Interaction and compatibility

1. New/Edit open an owner-centered Fluent modal using a detached `ProjectDeviceEditor`. Opening/typing/cancelling never changes retained devices, file bytes, browser or network. Dirty Cancel/close/application exit uses existing discard consent; Show/Copy alone is not a document edit.
2. Add/Save changes validates the complete proposed project with existing URL/reference/password rules before mutation. Failure preserves original and draft and focuses required fields. Edits preserve ID. Existing pending programmatic device drafts are rejected rather than overwritten.
3. Successful Add/Edit selects the committed row, clears search so it is visible, marks the project dirty and reminds the user to save. Existing atomic/conflict/backup/version/consent behavior stays on Save project file; no automatic disk write.
4. Preserve many-device search/grouping/confirmed deletion. The right panel shows one selection. List address is the explicit optional IP or a display-only URL host fallback; no stored IP is invented. Absent optional details do not reserve a large empty group.
5. Open preserves exact destination confirmation and URL/TLS warnings without probes or network changes. Linked Apply & Open retains frozen intent/NIC/review/elevation/verification/recovery. Modal creation/editing never opens or applies.
6. Preserve the accepted [password/v2 contract](DEVICE_PASSWORD_PROPOSAL.md): masked PasswordBox; explicit opt-in/show/copy/remove; reject empty/oversize values without truncation/implicit removal; plaintext/backup consent and separate initial v2 file; process-owned conditional clipboard cleanup. No login/token/TLS changes. Detail Show/Copy remain; removal occurs in Edit. Hide on selection/tab departure/deactivation/close and disclosure collapse. Synthetic fixtures only; never capture revealed passwords.

## Layout and acceptance

Reuse Card/FooterBar, PageTitleText/FieldLabel/CaptionText, NavigationListItem and native Fluent icons/controls. Modal normal 620x760, minimum 440x480; main minimum 760x520 and Project minimum 700x540. Body scrolls with fixed modal actions; left navigation narrows at smaller widths. Expansion motion must not misdirect keyboard/validation scrolling. No dependency, schema, persistence or broader navigation change.

Exercise real New/Edit/Add, multiple-device selection, invalid name/URL/IP/reference/password, same-ID edits, Cancel/owner-exit refusal and explicit linked apply with fake boundaries. Check both themes/sizes, field/disclosure names, name→URL tab order, password automation redaction, reachable actions, show/copy/removal/new state and existing regressions. Compare actual WPF captures to targets while preserving actual product typography/headers. Real clipboard/Narrator/high contrast/physical DPI/owner acceptance remain UNVERIFIED.

[Implementation review and exact checks](../testing/DEVICE_INTERFACE_UX_REVIEW.md).
