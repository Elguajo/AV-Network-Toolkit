# 04-T07 — legacy history/revert compatibility review

2026-10-31; base main abdd1d1c61dd4f0d6b39adfc24ee69ff46367cbc. Inspected HistoryEntry, HistoryRepository, MainViewModel.ApplyCoreAsync/RevertAsync and NetworkService/ElevationHelper.

Old history contains DHCP flags and old IP/mask/gateway, but no DNS source/ordered values, additional addresses, persistent state or routes. Missing fields are **unknown**, not empty or automatic. Revert keeps its current behavior; loading old arrays does not synthesize DNS or migrate data. Direct-write/backup-reset limitations remain as recorded in [legacy persistence review](LEGACY_PERSISTENCE_REVIEW.md).

The new project flow uses a separate typed recovery journal, never old history as a full snapshot. It must not append apparently fully recoverable entries to that old model or reuse RevertAsync to restore new snapshots. Recovery is explicit, uses fresh identity/current-state review, and verifies supported restored state; DHCP restores policy rather than a specific old lease. Unsupported/unknown recovery is manual and labelled unverified.

ApplyCoreAsync currently appends history after command completion in the same try block: a persistence exception can be presented as apply failure although the NIC already changed. This is confirmed source behavior, with no live reproduction. New flow separates pre-change journal failure (abort before apply) from final journal-update failure (network result retained). No sweeping legacy remediation or new completeness claim is authorized here. A later compatibility repair needs its own fixtures/review for old entries, sanitized IDs, atomic storage and user messaging.

The old temp-file elevated worker also remains legacy-only; no tested exploit is claimed. New typed requests never enter that generic argument-list path. Unit and protected-path checks must verify retained legacy semantics. Live revert/UAC/NIC testing remains NOT RUN without separate isolated-environment authorization.
