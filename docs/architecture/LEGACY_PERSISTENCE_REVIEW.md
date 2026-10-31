# 02-T06 — legacy persistence review, no remediation implemented

Date: 2026-10-31. Reviewed `ProfileRepository.cs`, `HistoryRepository.cs`, `AppPaths.cs` and their tests at code baseline 603cf24fa0ba1819c0ec10b6a57ef5cf57c3852e, unchanged by Phase 02 except unrelated test-enumeration seams elsewhere.

## Storage map and findings

- Source of truth: `%LOCALAPPDATA%/NetworkProfileManager/data/profiles_<sanitized-adapter-id>.json` and `history_<sanitized-adapter-id>.json`. Profiles remain unversioned Newtonsoft arrays with five slots; history retains up to 100 entries. App settings and one-shot migration marker share this folder.
- Confirmed integrity risk: `Save`/`Append` use direct `File.WriteAllText`; interruption, disk-full or concurrent saves can truncate/lose prior content. No previous-revision check or atomic replacement.
- Confirmed recovery risk: each `BackupAndReset` catches failed `File.Copy` and still attempts `File.Delete`. An unreadable/corrupt original can be the only copy. Existing `.bak` is overwritten on a later corruption event.
- History `Clear` intentionally deletes; IO failures are swallowed. Path sanitization may map different adapter IDs to the same filename. No new collision reproduction or live-data experiment was performed; review a compatibility-safe identity strategy before changing filenames.
- History lacks DNS/policy/routes/additional IPv4; persistence hardening must not invent recoverable fields. Phase 04 owns that separate review.

## Proposed separately reviewed remediation PR

Retain legacy filenames, JSON shape, five-slot import/count behavior, 100-entry retention and AppPaths migration semantics. Introduce atomic legacy writes with unique backups, preserve the original after failed backup, and return observable corruption errors rather than automatic destructive reset. Decide caller/UI handling and old `.bak` preservation explicitly before implementation. No project-to-legacy migration and no network-setting behavior in that PR.

Required isolated recovery fixtures (not implemented/run by this review): byte-for-byte old arrays/settings, UTF-8 BOM, missing/corrupt/truncated JSON, existing `.bak`, read-only/locked source, failed copy/flush/replace, disk-full/partial-write injection, two concurrent saves, sanitized-ID collision, interrupted migration and its marker. Assert originals survive failure; no fake PASS from skipping failures. Existing repository tests only establish their present cases; they do not prove this proposed recovery contract.

## Phase 02 disposition

Review deliverable complete; remediation remains unapproved. Existing repositories, migration and legacy editor/apply semantics were not changed. New project IO uses independent storage with tested failure preservation. Backups contain private project content and require the same local privacy/retention treatment as originals. No automatic backup pruning or customer-data fixtures were introduced.
