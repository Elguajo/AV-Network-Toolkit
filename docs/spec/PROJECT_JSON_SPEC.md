# Project JSON v1 — approved contract (ADR-009)

Owner approved the concrete format package on 2026-10-31. See [review package](PHASE02_FORMAT_REVIEW.md). Implementation status and execution evidence belong to [STATUS](../../STATUS.md) and [test matrix](../testing/TEST_MATRIX.md); this specification does not certify Windows apply behavior.

**FR-13 / IDEA-03:** owner accepted the separate [v2 password contract](DEVICE_PASSWORD_PROPOSAL.md) on 2026-11-02. This document and the v1 schema remain unchanged contracts for password-free files. V2 adds only an optional device password; first password-bearing save requires a separate new v2 file, preserving v1. Loaded v2 stays v2 after password removal; old v1-only applications reject v2. Unknown fields remain rejected in both versions. Do not put a password in v1, notes or URL. [Implementation evidence and limits](../testing/DEVICE_PASSWORD_REVIEW.md) distinguish local support from published 1.1.5, native clipboard and phase acceptance.

## Layout and ownership

One portable `project.avnet.json`, selected explicitly by the user, contains `schemaVersion`, `project`, `profiles[]`, `devices[]`. Open/Choose Folder never applies network settings, launches URLs or writes legacy AppData profiles. Project profiles have no five-slot limit; legacy five profiles per adapter and legacy JSON import/export remain unchanged. There is no automatic project-to-legacy mapping or merge-import in Phase 02.

Project ID and profile/device IDs are nonblank case-sensitive strings, unique across all records. New records use UUIDs. Normal edits and Save As preserve identity. Duplicate IDs, duplicate JSON property names and orphan references are rejected. Duplicate display names are permitted; identity is the ID, never a display name. IDs/refs are never automatically repaired or renamed.

## Profiles and intent

- `mode`: `static` or `dhcp`. `link-local` and static 169.254/16 remain rejected in v1. Phase 05 provides a separate transient manual action under [its contract](PHASE05_IMPLEMENTATION_CONTRACT.md); it does not extend/migrate the approved portable format.
- Static requires an `ipv4` object: canonical dotted-quad `address`, integer `prefixLength` 1–32, explicit `gateway` and `dns`.
- Gateway must be a different valid host in the same subnet, or explicit JSON `null` for no gateway. Missing gateway and empty string are invalid. There is no inheritance from the previous adapter state. The editor labels blank as **no gateway**, serializing it to null.
- DNS is explicit: `static` with 1–2 distinct valid IPv4 servers, or `automatic`/`leave-unchanged` with an empty server array. An editor secondary without primary is rejected. No automatic translation to legacy empty DNS.
- DHCP has no `ipv4` block and means automatic IPv4 plus automatic DNS intent. IPv6 is outside the format/application scope.
- Reject unspecified/loopback/multicast/reserved host addresses. For prefixes <=30, reject subnet network/broadcast host and gateway addresses. /31 and /32 with null gateway may be stored; this does not certify OS apply support.
- `adapterHint.preferredAlias` is optional display guidance only, never a selection or stable OS identity binding.

Phase 02 stores intent only. In particular null-gateway route removal and static-IP/automatic-DNS cannot be implemented by omitting legacy command arguments. Phase 04 must prove exact requested/observed state and recovery before enabling project apply; unsupported cases must be blocked before mutation.

## Device storage and URL security

Devices store ID, name, full `webUrl`, optional type/manufacturer/IP/notes and nullable/optional `profileId`. Device management, HTTP/IP-host mismatch warnings and browser launch belong to Phase 03. Non-secret URL text round-trips verbatim, including path/query/fragment, ports and escaping.

Only valid absolute http(s) URLs without userinfo, whitespace, controls or backslashes are accepted. Query and fragment are percent-decoded for inspection (not storage), with bounded decoding; excessively nested encoding is rejected. Suspected credential keys are matched case-insensitively, including fragment routes and hyphen variants: password/passwd/pwd/token/access_token/refresh_token/id_token/api_key/apikey/secret/authorization/auth/session/sessionid/cookie/code/signature/sig/key. Such URLs are rejected without a persistence override. Error messages contain code/field path, never the URL or rejected value.

This deliberately conservative check may reject harmless keys, and cannot detect arbitrary secrets hidden in unrelated free text. Do not put credentials in names, descriptions or notes. There are no credential/password/token fields; unknown properties are rejected. No URL is visited during validation, no DNS resolution, TLS bypass or browser integration occurs.

## Validation and compatibility

[Public schema](../../schemas/project.avnet.schema.json) is draft 2020-12, embedded in the app. JsonSchema.Net 7.4.0 is pinned, with `EvaluateAs = SpecVersion.Draft202012` and `RequireFormatValidation = true`. Explicit canonical IPv4 patterns supplement the library's permissive IPv4 format implementation. Independent Python checks use Draft202012Validator with an explicit FormatChecker.

Separate semantic validation enforces identities, references, host/subnet/gateway and URL-security rules. UTF-8 must be valid; comments/trailing commas are rejected. Input/output is limited to 8 MiB and depth 64 to bound parsing work; there is no arbitrary profile count cap.

Version 1 rejects unknown properties at every object level and unsupported schema versions. Unsupported/corrupt files are never normalized, reset or resaved automatically. There is no released predecessor format to migrate; v0 and future versions receive explicit incompatibility errors. Legacy arrays stay on their existing separate path, not a hidden migration path.

## Files, saves and recovery

- Open reads/validates only; failed open retains the active document/draft. Choose Folder chooses a future save location, not a recursive folder import.
- File paths must be explicit absolute local paths; UNC/device paths, alternate streams and existing junction/symlink components are rejected. Network shares, concurrent multi-user editing, hostile local filesystem races and atomicity on arbitrary third-party filesystems are not certified.
- New/Open/close protect unsaved document/editor changes with discard confirmation. Save As includes pending valid profile edits; invalid edits block saving. Existing target overwrite always requires explicit confirmation showing its exact path.
- Existing targets require their observed SHA-256 revision. Saving back to an opened path retains its original revision; intervening external edits/deletion cause conflict rather than blind overwrite.
- Cooperative savers use an exclusive DeleteOnClose lease. Write a unique same-directory temp file, validate before writing, and flush bytes to disk. Before overwrite, copy exact old bytes to a unique backup and flush it. Failed serialization/temp write/flush/backup/replace never triggers delete/reset of the original.
- Replace atomically with Windows `File.Replace`, retaining a second backup in that operation. If an uncooperative external replacement races after the last revision check, its bytes survive in that backup and the app signals `ConcurrentReplace`; the target may already contain the new document. Reopen/compare all versions before another save. No claim of universal compare-and-swap against arbitrary editors.
- Normal successful overwrite keeps one unique `.bak` of exact previous bytes; redundant atomic backup is removed only if byte-identical to that complete backup. Backups are not auto-pruned. Growth is visible via the backup path; user manages retention.
- No automatic restore or corruption cleanup. Open a backup explicitly (Open accepts `.bak`) and Save As to a new path. Preserve the corrupt/conflicting source and any partial backups for comparison; only valid project files may be used as recovered documents.
- A crash before replace leaves the original plus possible temp/backup evidence; after replace leaves a complete target and prior version. Best effort cleanup removes only this operation's temp file. Power-loss behavior ultimately depends on filesystem/storage guarantees; not fault-injection certified on hardware.

No project repository calls AppPaths, ProfileRepository, HistoryRepository, netsh, UAC or browser APIs. Legacy persistence review is [02-T06](../architecture/LEGACY_PERSISTENCE_REVIEW.md), not authorization to fix/migrate legacy files.
