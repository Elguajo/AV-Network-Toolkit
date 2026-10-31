# Phase 02 format proposal — approved by owner (ADR-009)

Date: 2026-10-31 (Asia/Tashkent). Task: 02-T01. Owner approved this package on 2026-10-31. Implementation and acceptance evidence remain separate.

## Proposed version 1 contract

| Decision | Proposal | Tradeoff |
|---|---|---|
| Layout | One `project.avnet.json` containing project, profiles and devices. Folder selection only chooses a location; no directory traversal/import. | One atomic transaction and portable file; less convenient for simultaneous multi-person editing. |
| Legacy mapping | Independent project collection, no five-item cap. Open/import never writes legacy slots. Phase 02 does not offer project-to-legacy copying or apply; existing five slots stay untouched. | Avoids lossy gateway/DNS mapping. Explicit mapping/apply needs Phase 04 review. |
| IDs and conflicts | Nonblank case-sensitive string IDs, unique across all project/profile/device records. New IDs use UUIDs; edit and Save As preserve IDs. Reject duplicates/orphan refs. Open replaces the active project only after successful validation and discard confirmation. No merge-import in v1. | Rejecting conflicts is predictable; no automatic repair/renaming UI. |
| Unknown/future data | Reject unknown properties at every level and unsupported versions with an incompatibility error. Never normalize/resave unsupported files. Reject duplicate JSON property names as ambiguous. | Extensions require a version change; no silent field loss or complex extension bag. |
| Link-local | Version 1 accepts only static/dhcp; reject reserved `link-local` and static 169.254/16 until Phase 05 defines it. | Such files cannot be opened for editing yet. No fallback to static/DHCP. |
| Gateway | Static IPv4 requires the gateway property: IPv4 string or explicit null. Null declares no gateway for this profile; missing/empty string is invalid. No inherited value. Phase 02 only stores intent; route removal/recovery must be proved in Phase 04 before apply. | Strict authoring; avoids hidden defaults. |
| DNS | Static IPv4 requires explicit `automatic`, `static`, or `leave-unchanged`; automatic/leave-unchanged require empty servers, static requires 1–2 distinct valid IPv4 servers. DHCP implies automatic address and DNS, with no ipv4 block. | Third mode faithfully represents legacy empty DNS. Static-IP/automatic-DNS apply remains blocked until Phase 04 proof; no mapping to empty legacy fields. |
| URLs | Preserve non-secret absolute http(s) URL text exactly, reject userinfo, controls/whitespace, unsafe schemes. Block persistence if decoded query/fragment has credential-like parameter names (`password`, `passwd`, `pwd`, `token`, `access_token`, `refresh_token`, `id_token`, `api_key`, `apikey`, `secret`, `authorization`, `auth`, `session`, `sessionid`, `cookie`, `code`, `signature`, `sig`, `key`), including fragment routes/parameters. Error reports only field path, never value. No override in v1. | Conservative detection can reject innocent `code`/`key` parameters; cannot prove arbitrary free text is secret-free. No credential fields; owner must avoid secrets in names/notes too. |

Static host validation will reject unspecified, loopback, multicast/reserved, subnet network/broadcast addresses (for prefixes <=30), and gateway equal to the host or outside its subnet. /31 and /32 can be stored with null gateway; this is storage support, not Windows apply certification. Prefix /0 is rejected for a host profile. DNS servers reject unspecified/loopback/multicast/broadcast. Samples use RFC5737 addresses.

## Smallest implementation after approval

- Separate project models/validator/store and project viewmodel; no changes to legacy repositories or netsh/UAC/history logic.
- Localized Project commands and a small project profile editor/list using existing WPF/MVVM patterns. Explicit New/Open/Choose Folder/Save As; path shown; no device-management UI or URL launch.
- Draft 2020-12 structural validation with explicit format assertions, plus semantic validation; evaluate a compatible maintained validator before adding a production dependency. Tests also validate the public schema independently.
- Same-directory temporary file, durable flush, atomic replace with unique backup on existing target; abort on backup/replace failure, never delete original. Explicit overwrite confirmation; external edit conflict check and file locking. No automatic recovery overwrite; open backup explicitly and use Save As.
- Preparation tests/tooling first, then format/schema tests, store failure/round-trip tests, project viewmodel tests, build and isolated regression. Windows UI/live network checks reported separately.

## Review requested

Approve this package or name changes. In particular: third DNS mode, strict rejection of unknown fields/link-local, no legacy mapping in Phase 02, and conservative URL-secret blocking are product choices. Phase 02 start is already authorized; this review is specifically required by 02-T01 before schema implementation.
