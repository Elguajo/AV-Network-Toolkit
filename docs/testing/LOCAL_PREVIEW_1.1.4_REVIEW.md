# Local 1.1.4 Preview — owner manual-test candidate

2026-11-01, Asia/Tashkent. Fork https://github.com/Elguajo/AV-Network-Toolkit, main, inspected base `4c0a3aa`. Owner requested a version bump with all completed changes and the local executable path. Phase 07 remains AWAITING_REVIEW; no GitHub push/release or agent live-network execution.

## Preparation

Version/file/assembly updated to 1.1.4/1.1.4.0, changelog/notes and manually dispatched release-workflow version gate synchronized. Application behavior and data formats/locations unchanged. Source commit is resolved through `git log -1 --format=%H -- app/NetworkProfileManager.csproj`; package evidence will pin that complete SHA.

Build and local package checks IN PROGRESS. No build/test/package success claimed at preparation. Use existing SDK 10.0.403 and pinned Inno Setup through scripts/build_packages.ps1, producing a fresh out/LocalPreview-1.1.4 directory. Owner will launch the self-contained payload manually.
