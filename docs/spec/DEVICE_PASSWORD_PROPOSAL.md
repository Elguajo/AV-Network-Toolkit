# Device passwords — FR-13 / IDEA-03 review proposal

2026-11-02, Asia/Tashkent. **PROPOSED / OWNER_REVIEW_PENDING; no implementation or schema change.** Inspected fork https://github.com/Elguajo/AV-Network-Toolkit, `main`, `a75a3acb4c533570ba3ce705e012011681d6e63e`, initially clean. Phase 07 remains AWAITING_REVIEW.

## Authorization and review boundary

Owner requested starting local work on optional device web-interface passwords, with explicit show/copy actions. [ADR-024](../../decisions/DECISION_LOG.md#adr-024--permit-device-passwords-in-project-json-2026-10-31) already permits plaintext portable storage. Do not ask again whether passwords may be stored. This proposal settles the required version/compatibility review before changing the working format, as required by [AGENTS.md](../../AGENTS.md).

Approve the concrete v2/transition/UI/clipboard contract below before implementation. In particular, the first password-bearing save creates a separate new v2 file; it cannot replace the opened v1 file. This preserves a usable v1 original but gives two independent project copies. In-place v1 upgrade is the alternative requiring an explicit different owner decision. No automatic downgrade is proposed.

Local authorization does not permit real-file migration, live network/UAC/browser actions, installation, publication, new shared resources, tokens, automatic login or TLS bypass. DATA-01 and application updates are independently owned. Phase acceptance remains separate.

## Existing requirements and source evidence

- [FR-13/product-fit criteria](../PRODUCT_FIT_REVIEW_RU.md#требование-к-паролям-что-считаем-готовым): optional password, exact round-trip, clear plaintext/copy/backup warning, hidden by default, separate access, no URL/log/diagnostic/recovery exposure, predictable editing/removal and clipboard policy agreed before code.
- Login is only part of a **proposed** interface there. The current request names passwords; this minimal extension adds no login field. Adding login needs a separate scope decision.
- [Project DTO](../../app/Models/NetworkProject.cs), [parser/writer](../../app/Services/ProjectJson.cs) and [schema](../../schemas/project.avnet.schema.json): version 1 only, unknown properties rejected, duplicate properties rejected recursively, 8 MiB/depth-64 limits; no credentials. `ProjectDevice` is a record: adding a password property without a redacted string representation would expose it through generated `ToString()`.
- [ProjectStore](../../app/Services/ProjectStore.cs): create-only by default, expected-revision conflict checks, exclusive cooperative lease, flushed same-directory temporary write, exact-byte unique backup and atomic replace. Late external replacement preserves both versions and reports conflict. Reuse these guarantees.
- [Device VM/editor](../../app/ViewModels/ProjectViewModel.Devices.cs): detached draft, Keep validation, explicit discard/delete/launch confirmation; search uses a named noncredential field list. Apply & Open currently deep-copies the whole project and passes a `ProjectDevice`; credential data must be removed at this handoff.
- [Project VM](../../app/ViewModels/ProjectViewModel.cs): validates before loading; saves pending drafts before disk save, retains dirty state on failure/cancel; `Current()` preserves project metadata. No project collection merge-import exists. Legacy profile import/export is a different format and must remain unchanged.
- [Browser launcher](../../app/Services/BrowserLauncher.cs): only validated original HTTP(S) URL goes to ShellExecute. [Apply VM](../../app/ViewModels/ProjectApplyViewModel.cs) retains the device, but the [coordinator](../../app/Services/ProjectApplyCoordinator.cs) accepts destination separately; privileged requests/journals contain typed network intent, not device credentials. Preserve this separation.
- Existing tests: [JSON](../../test/ProjectJsonTests.cs), [parallel validation](../../test/ProjectJsonConcurrencyTests.cs), [store failures/races](../../test/ProjectStoreTests.cs), [device drafts/browser](../../test/DeviceDirectoryTests.cs), [project VM](../../test/ProjectViewModelTests.cs) and [safe WPF harness](../../scripts/ProjectUiSmoke/Program.cs). Runtime is currently .NET 10/WPF, pinned SDK 10.0.403; old phase references to .NET 8 are historical.

## Proposed wire format and exact states

Keep the strict v1 schema unchanged. Add a separate strict v2 schema and select the validator by the required integer `schemaVersion`. V2 changes only the version constant and adds optional `password` to each device. All other fields, IDs, network semantics and URL restrictions stay the same. Unsupported versions/unknown fields are rejected before replacing the current document or writing a file; no extension bag or lossy repair.

Illustrative device fragment only, not a complete project; the containing project has `schemaVersion: 2`:

```json
{
  "id": "synthetic-device",
  "name": "Example panel",
  "webUrl": "https://192.0.2.20/panel",
  "profileId": null,
  "password": "SYNTHETIC-ONLY"
}
```

| State | JSON / editor contract |
|---|---|
| Not stored | Property absent; nullable DTO value is null and omitted on write. New-device opt-in unchecked. |
| Stored | Nonempty string, maximum 1024 Unicode scalar values. Preserve quotes, Unicode, leading/trailing and whitespace-only spaces exactly; never trim or normalize. Reject control characters and malformed Unicode with a value-free error. |
| Empty input | `""` and JSON null are invalid stored values. Opt-in with empty input blocks Keep/Save, preserving the draft and previous password. No implicit removal. |
| Explicit removal | Remove action clears the draft password and disables storage; Keep/Save removes the property from the current project. Cancelling/discarding the draft restores the retained device. Old backups/copies remain unchanged. |
| Missing versus removed | Identical on disk: no credential. Removal is an editor action, not a persistent tombstone; there is no synchronization protocol. |

The proposed 1024-scalar bound and rejection of empty/control-bearing passwords are product constraints to review, not claims about every device vendor. A blank device password needs no stored secret; support for an explicit stored empty password would require changing this table before code.

## Compatibility and transition

| Input/action | New application | Existing v1-only application |
|---|---|---|
| Open/edit/save v1 without adding password | Stays v1; no startup/open rewrite | Continues to work |
| First password saved from v1/new project | Explicit warning and new, create-only v2 destination; reject same source path and existing destinations. Original v1 remains byte-identical. Cancel leaves current path/revision/draft/dirty state recoverable. | Original v1 remains usable; v2 fails with unsupported-version error |
| Open/save/copy v2 | Preserve v2 and every password through profile-only edits, device edits, Keep, Save and Save As | Reject before loading; cannot round-trip v2 |
| Remove final password from v2 | Stay v2; no automatic downgrade | Still rejects v2 |
| Malformed/future/unknown properties | Reject without replacing current state or rewriting source | No new compatibility guarantee |
| Save v1 over an existing v2 target | New writer rejects downgrade; Save As must choose another path | Old application cannot be retroactively fixed: an explicitly confirmed Save As of another v1 project can replace v2, retaining its existing backup behavior |

Use v2 only when a password is first retained/saved, not merely when shown, copied, or a checkbox is clicked and cancelled. A v1 document with retained password intent must be validated as a proposed v2 document in memory; disk path changes only after successful save. Subsequent removal after a completed v2 save never downgrades it. Repeated save is idempotent with respect to version.

The new writer must validate an existing overwrite target for supported version/structure before replacing it, refusing future/unknown-field project targets rather than losing unrecognized data. Existing explicit corrupt-file replacement behavior may remain only for genuinely malformed files after the existing confirmation/backup flow; syntactically valid unsupported project documents are not classified as replaceable corrupt files. Check target revision again under the save lease so a target changed to v2/future data cannot bypass the guard. A conflicting atomic replacement retains its existing recovery backups and signals the conflict, never success.

No bulk migration, automatic scanning, original deletion or v1 export-with-silent-password-stripping. No new import/export into legacy profiles. A separate credential-free v1 export, if later wanted, needs its own explicit contract.

## UI and consent

Use the existing [DESIGN.md](../../DESIGN.md) device editor, themed labelled controls, scrollable body and fixed existing actions; no new screen or redesign. Add an optional Password group with an unchecked **Save password in project file** opt-in, masked input and separately named **Show / Hide**, **Copy**, **Remove** actions. For a loaded password, the opt-in reflects its existing presence; loading must not delete it or ask for consent to merely read the project.

Showing/hiding and copying never mark the document dirty or save it. Re-mask on device/editor replacement, project replacement, leaving the device section, view unload and window deactivation; do not persist reveal state. Show/Copy operate on the current draft, never a stale selected record. Failure/cancel/discard and selection changes use existing draft guards. No password in tooltip, accessible label/help text, list row, search/group, destination preview or normal status. Only the deliberate revealed value or clipboard contains the plaintext; masked input must not expose its value through a normal text automation peer.

Keep a visible warning with a stored password: **Anyone with this project file or its backups can read saved passwords. Share it only with people who should have them.** Before each disk save containing passwords, confirm this warning without showing values; initial v2 save also names the old-version incompatibility and separate-copy rule. If removal/overwrite will create a backup containing an old password, warn that the backup retains it even if the new file has none. Denial cancels disk IO, never silently strips data. Reuse existing overwrite confirmation where appropriate without skipping either consent. No share command exists; do not invent one.

## Clipboard and exposure boundaries

Copy is explicit. Proposed cleanup: **30 seconds**, and a best-effort attempt on application exit. A process-owned clipboard service outlives the device view. Clear only if both the clipboard ownership marker and Windows clipboard sequence still match this exact copy; also verify the text. A later copy, including identical text from another app, must survive. Repeated password copy replaces the previous timer/ownership token. Clipboard busy/error/changed/closed cases never display values or clear unrelated content; report only a generic status. Use injectable clipboard/time boundaries in automated tests.

Explain that clipboard history, cloud clipboard, password managers and other applications may retain copies; cleanup is best effort and not secure erasure. No attempt to change Windows clipboard/history settings. Exact clipboard API/control integration must use current primary documentation during implementation.

Override credential-bearing DTO string output with a constant redaction, including nested presentation/snapshot formatting as needed. Do not serialize a full device to diagnostics or a recovery request. Strip password before the Apply & Open event and defensively at the Apply VM entry; only the existing validated URL reaches browser/process boundaries. Password must not enter journal/wire payloads, CSV, scanner/mDNS/probe inputs, exception messages, logs or normal UI automation output. Deliberate JSON project files/temporary atomic-save payloads/backups and deliberate Show/Copy are the only added plaintext outputs. Do not claim secure memory wiping of managed strings.

Do not infer that an arbitrary URL is secret-free merely because its parameter names pass the existing validator. Preserve the URL policy; do not automatically substitute a password anywhere. Tests must prove the new field is never interpolated into URL/query/browser arguments. Arbitrary secrets manually pasted into unrelated metadata remain outside automated detection guarantees.

## Implementation and verification after approval

1. Add reviewed v2 schema/resource/DTO, strict dispatch, exact password validation and redacted record formatting. Keep v1 schema/example and legacy contracts unchanged.
2. Preserve version/password in clones, DTO/editor builds and every project save path; implement the separate initial v2 save and overwrite compatibility guards using existing atomic IO.
3. Add opt-in/masked field, show/copy/remove, consent copy and clipboard lifecycle; reduce the Apply & Open handoff to credential-free device data.
4. Add focused regression tests using synthetic credentials only; security/storage review of the actual diff, then required checks and durable evidence. Do not claim support from this proposal.

| Acceptance area | Required evidence |
|---|---|
| Compatibility | Real v1 fixture unchanged on open and semantically identical after save; v1 rejects credentials; v2 accepts only approved field; frozen v1 schema/parser rejects v2; future/unknown/duplicate/type/depth/size rejection. |
| Exact round-trip | Unicode/non-BMP/quotes/backslashes/spaces and maximum/over-limit; absent/empty/null/control/invalid Unicode cases; parse → clone → edit unrelated profile/device → Keep → Save As → Open; retained v2 after final removal. |
| Transition/cancel | New destination required, source byte identity, denied consent, cancelled dialog, same/existing target rejected, successful path/revision switch only after write; cancelled/failed saves keep credentials and dirty intent. |
| Storage/failure | Synthetic old/new password bytes through every existing injected write failure, flush/backup/replace failure, external changes, cooperative lease and late replacement; unknown-field/future/v2 downgrade overwrite refused, originals/backups intact. |
| Draft/UI | New/edit/Keep/remove, discard accepted/denied, selection/filter/group/tab/project/close transitions; show/copy do not dirty, reveal resets, input clearing does not silently delete. Light/Dark normal/minimum WPF captures use placeholders or absent secrets, never revealed passwords. |
| Clipboard | Fake time and ownership/sequence/text: timeout, overwritten different/same text, repeated copy, view close, app exit, busy/failure. No real clipboard in unit/CI. Native clipboard/history behavior stays NOT RUN until separately exercised with synthetic data. |
| Leakage/network invariant | Search/group/ToString/errors/validation/normal automation/journal/wire/CSV/launch arguments exclude synthetic sentinel; explicit reveal/copy are allowed only in isolated noncaptured checks. Open/save/reveal/copy/remove never invokes network/elevation/apply; browser receives exact original URL only. |
| Regression | Release build, full safe unit suite, planning validator/tooling tests and existing WPF harness; no live network, real URLs, UAC or ordinary app startup. Record exact commands/results and NOT RUN limits; owner/native/phase acceptance separate. |

## Current evidence and unresolved decisions

Completed: source/requirements/compatibility inspection and this concrete proposal. **No working schema, model, UI, password support, real file migration or runtime behavior changed.** Baseline compatibility facts above are source evidence; the new acceptance matrix is not a test result.

Owner review required for the proposed v2 contract, particularly separate first-save copy, no automatic downgrade, invalid empty password and 30-second conditional clipboard cleanup. Approving this packet authorizes bounded local implementation and synthetic verification only; native/phase/release/shared-resource gates remain separate. No skill imposes an additional permission gate: this review comes from AGENTS.md's versioned reviewed-extension requirement and the task's explicit format/compatibility checkpoint.
