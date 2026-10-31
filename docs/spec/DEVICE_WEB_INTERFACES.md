# Device Web Interfaces — Phase 03 spec

**Later owner refinement (ADR-024 / FR-13):** optional device passwords in project JSON are now permitted. The no-password wording below describes the original Phase 03/v1 boundary; it does not prohibit the future feature. Current model/editor/schema still have no credential fields. Versioning and explicit credential UI are pending under [the product-fit review](../PRODUCT_FIT_REVIEW_RU.md); URL validation, no automatic login and no TLS bypass remain unchanged.

## User flow

1. Choose project.
2. Add device: name, optional manufacturer/type, optional IP, full URL, optional related `profileId`, and notes (no secrets).
3. List/search/group devices by project, type and network profile. One profile may contain many device links.
4. **Open Web Interface** launches default browser **without** changing adapter settings.
5. Edit/delete a device entry only after user action. Preserve full deep link, e.g. `https://192.0.2.44/web/panel/index.html#!/main` or `http://198.51.100.21/en_US/admin`.

## Validation & UX

- Enforce `http:` or `https:` scheme; reject `javascript:`, `file:`, other schemes, embedded username/password and malformed hosts.
- Display full destination before open (tooltip/preview) and warn on unencrypted HTTP.
- Do not suppress invalid/self-signed certificate errors. The external browser owns certificate checks.
- Do not run untrusted browser commands via shell interpolation. Use an approved safe URL-opening abstraction, tested for special characters and fragments.
- Do not save passwords/cookies in project files. Audit/report URLs likely to contain secret tokens.
- A configured device IP may differ from URL host (reverse proxy/FQDN): warn, don't silently rewrite.
- Missing profile reference: show 'unlinked' and let user choose/relink, no automatic network application.
- Unreachable web endpoint is not grounds to mutate network settings without explicit permission.

## Acceptance examples

- One profile linked to 24 device records.
- Distinct devices can share the same IP if their **full URL or port differs**; do not deduplicate solely by IP.
- URL with `:8443`, nested path, query and `#!/main` survives import/export and launches unchanged.
- URL with `file://` / embedded credentials rejected; HTTPS warning never automatically bypassed.

## Phase 03 implementation contract — ADR-015

- The Devices tab extends the existing Project window. It uses the existing `ProjectDevice` v1 fields and UUID identity. Keep device updates the in-memory directory; Save as persists it with the existing project store, explicit overwrite/revision checks and backups. No new schema or migration. Empty optional fields become null/omitted; URL text is never trimmed or normalized.
- Search covers name, type, manufacturer, IP, full URL, notes and related profile name, case-insensitively. Group by existing type or related profile inside the current project; no persisted tags or group property. Profile groups use stable IDs, so equal display names do not merge different associations. Profile choices/group headers expose IDs as tooltips to distinguish duplicate names.
- Device drafts retain identity after validation/save errors. New device, selection change, New/Open project and window/application exit protect pending edits. Profile and device drafts are independent; tab changes/search/grouping retain them. Save as includes both valid pending editors. Search may hide the selected row while its editor stays open; clearing search restores it.
- A saved `profileId: null` is explicitly unlinked. Imported orphan references remain rejected under ADR-009, retaining the current document. An unavailable editor reference blocks Keep/Save/Open until the user chooses an available profile or explicitly unlinks. Deleting a referenced profile is blocked, including references in a pending device draft. No fallback profile is selected/applied.
- Open Web Interface requires a selected retained device. It validates the current device draft (including URL and profile reference) without silently keeping or saving edits. Confirmation shows the exact destination; HTTP, differing IP/URL host (including FQDN/reverse proxy) and unlinked states add warnings. Cancel launches nothing. Device IP is never substituted into a URL.
- Validation reuses ProjectJson's approved URL policy, including bounded decoding and credential-like query/fragment key rejection. Unsafe destinations are excluded from preview/confirmation and errors contain only a code/field. Raw text the user is editing remains in its input field; errors never repeat it.
- The launcher rechecks that same policy and passes the original string as `ProcessStartInfo.FileName` with `UseShellExecute=true`, without command arguments, a named browser or command-shell interpolation. Launch errors are redacted. A successful API return means an address handoff only; the browser/OS handler can normalize it afterward and actual browser navigation is UNVERIFIED until a separately authorized live check.
- There are no DNS/HTTP/ping probes or reachability claims. Status says the address was handed to the browser and reachability was not checked. External browser owns TLS/certificate UI; the application never bypasses it or offers credential handling. Phase 03 did not include Apply & Open; the separate Phase 04 action is now described in [its contract](PHASE04_IMPLEMENTATION_CONTRACT.md), without changing simple Open semantics.

Execution evidence, limitations and owner review gate: [Phase 03 review](../testing/PHASE03_REVIEW.md).
