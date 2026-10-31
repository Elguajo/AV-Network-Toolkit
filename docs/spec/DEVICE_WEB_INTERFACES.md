# Device Web Interfaces — Phase 03 spec

## User flow

1. Choose project.
2. Add device: name, optional manufacturer/type, optional IP, full URL, optional related `profileId`, and notes/tags (no secrets).
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
