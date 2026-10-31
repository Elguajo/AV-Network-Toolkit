# Draft project JSON format — **NOT implemented**

**Proposed filename:** `project.avnet.json` under a user-selected AV project folder.

- Project file is **separate** from existing `%LOCALAPPDATA%\NetworkProfileManager\data\profiles_<adapter-id>.json` files. Never repurpose these without a migration plan.
- Root `schemaVersion: 1`; require stable IDs; preserve unknown fields or report incompatibilities, **never silently discard** them.
- `profiles[]`: ID/name, `mode` (static/dhcp/link-local), optional adapter hint, IPv4 static settings if static.
- `ipv4.prefixLength` is 0–32; UI can show/convert dotted-decimal subnet mask. Additional semantic validation rejects nonsensical addresses/masks and gateway choices.
- `ipv4.gateway: null` means **no gateway** (do not add one). Importer must not interpret absent gateway as an inherited previous gateway.
- `ipv4.dns.mode = automatic` or `static`; static requires one/two validated addresses. The meaning of automatic DNS on static IP needs implementation/Windows API proof; it must not silently leave stale manual DNS.
- `adapterHint.preferredAlias` is display guidance only; on a different machine it must not auto-bind a NIC.
- `devices[]`: ID/name/type, optional IP, optional referenced `profileId`, required full `webUrl` for browser launch. References must resolve or be explicitly flagged as orphaned.
- **URL:** preserve full path/query/fragment verbatim through read/write; support `http` and `https` only. Warn if HTTP or if IP differs from URL hostname; keep user override deliberate.
- Project JSON must contain **no username, password, cookies, session token or secret browser URL query values**. If a URL contains a credential-like token, warn/block its persistence pending a reviewed policy.
- Not suitable for storing networks as publicly committed sample data; private projects remain outside public Git repository.

## IO policy

Open = parse and validate **without changing the OS or app profile store**. Save = write temporary file in same directory, flush and replace safely, retain a backup of previous content as appropriate. The user chooses project folder; any overwrite requires clear context and permission. Handle file locks, invalid JSON, future versions, duplicate IDs and missing references predictably.

## Example and validator

- `examples/project.avnet.json` contains **documentation-range IPs**, not production endpoints.
- `schemas/project.avnet.schema.json` is a draft structural validator; IP subnet/gateway and security semantics require additional application-level validation.
- Schema is intended for review in Phase 02 and may change by an explicit recorded ADR before implementation.

## Phase 01 findings and Phase 02 decision gate

This remains a draft. ADR-006 authorizes documenting decisions to make, not choosing their outcomes. See [baseline findings F02/F05/F07/F10](../architecture/BASELINE_AND_SOURCE_MAP.md) and 02-T01 through 02-T06.

- **Identity:** project/profile/device IDs must remain stable across normal edits. The legacy dialog creates a new `IpProfile` on edit; do not use that identity lifecycle for linked project records. Define duplicate IDs, imported conflicts and explicit copy semantics before implementation.
- **Storage layout — NEEDS_DECISION:** the draft schema has `devices[]` in one project file; earlier roadmap wording suggested a separate catalogue. Choose same-file or separate-file storage and its consistency/recovery rules in 02-T01; this adoption does not choose or change the schema.
- **Legacy mapping — NEEDS_DECISION:** define whether project profiles apply directly or are explicitly mapped into saved adapter slots. Preserve the existing five-slot workflow and reject overflow instead of truncating silently.
- **DNS:** legacy empty primary means leave current DNS unchanged, not automatic. A secondary without primary is ignored in that path. Do not map explicit project automatic/static DNS to those legacy defaults. Decide whether a leave-unchanged policy belongs in the project format; no third mode is added by this document. Unsupported automatic-DNS/static-IP combinations must fail before mutation until proved on Windows.
- **Gateway:** draft `null` means no gateway; define missing/empty values and exact removal/preservation semantics for affected routes. Omitting an argument in current `ApplyStaticIp` is not proof of the intended resulting OS state.
- **Compatibility — NEEDS_DECISION:** choose unknown-field preservation versus explicit incompatibility rejection. Current `additionalProperties: false` rejects extensions; never deserialize-and-resave while silently losing them. Future versions require a declared read/save policy.
- **Reserved mode — NEEDS_DECISION:** the draft accepts `link-local` before Phase 05 exists. Choose reject, read-only preservation or another reviewed unsupported-mode behavior; never execute it as static/DHCP by fallback.
- **URL secrets — NEEDS_DECISION:** retain full non-secret URLs, reject embedded credentials, and settle the policy for suspected secret query/fragment values before persistence. The schema's http(s) pattern does not enforce these semantics.

## Required IO and validation evidence (02-T04/02-T05)

Test malformed JSON, full JSON Schema draft 2020-12 constraints with format enforcement explicitly configured, and application semantic checks separately. Fixtures cover IDs/references, subnet/gateway constraints, DNS policy and exact URL round trips. Syntactic parsing or matching schemaVersion is insufficient.

For new project storage, demonstrate that failed serialization/temp write/flush/backup/replace leaves the original recoverable. Define behavior for file locks, permissions, interrupted writes and concurrent external edits. Never delete the only original after failed backup. Restore must be explicit and preserve evidence of corruption. Legacy remediation belongs to the separately reviewed 02-T06 scope; no migration is authorized here.
