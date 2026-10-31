# Licensing and attribution constraints

- Base fork: `rmzncakir/network-profile-changer` identifies its software license as **MIT**, with `LICENSE` present in the inspected upstream repository. Preserve copyright/permission notices and comply with terms when distributing derived copies.
- Feature reference: `mefranklin6/AV-Network-Toolbag` had **no LICENSE file** in the inspected tree on 2026-10-31. Public GitHub visibility or GitHub's built-in fork option does not grant broad permission to copy source/assets into another project.
- Agents may inspect **published behavior descriptions** and build original implementations. Do not paste/port Dart code, binaries, artwork or README content from the unlicensed repository.
- Seek explicit permission from the AV Toolbag author if actual code reuse is desired; record terms before using any supplied code.
- Before release, check any *new* dependency's license, supply-chain status, and redistribution conditions. Record notable notices in release docs.

This is a project policy note, not legal advice. Recheck repository license status during Phase 01 and before publication.

## Phase 01 evidence (2026-10-31)

Local fork LICENSE at `603cf24fa0ba1819c0ec10b6a57ef5cf57c3852e` contains MIT and copyright 2026 rmzncakir, retained unchanged. GitHub metadata confirmed parent rmzncakir/network-profile-changer. The AV Toolbag recursive tree at `0c171e296bfe819b79ec40f0caf34e41cd5751f2` (77 entries, not truncated) contained no LICENSE/COPYING/NOTICE filenames. This bounded file-tree observation does not establish copying permission. No code/assets copied; policy unchanged. See [baseline provenance](../architecture/BASELINE_AND_SOURCE_MAP.md).

## Phase 02 dependencies

See [added dependency notices](THIRD_PARTY_NOTICES.md). The exact restored package metadata/licenses were inspected; JsonSchema.Net 7.4.0 and its resolved transitive dependencies declare MIT. No AV Toolbag code/assets were used. A newer binary with additional maintenance-fee terms was evaluated and excluded before delivery.
