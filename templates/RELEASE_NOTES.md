# Product-first, engineering-backed release notes

Use this template for a specific, evidence-backed software release. Start from the previous **published** version, not an unpublished candidate. Adapt the number of highlights and omit inapplicable sections; the first fork release needs a product introduction rather than an artificial compare link.

For GitHub, keep the product/version in the release title field. The body deliberately has **no duplicate H1**. In a standalone document, supply a title through its surrounding index/preview or use one only when it will not duplicate the host's title.

The fenced text below is a drafting template, not publishable content. Replace every brace-delimited field with verified facts/URLs or remove the inapplicable item. Do not publish placeholders.

```markdown
> **Release status: {channel}.** {Most important remaining limitations.}

> **Important update:** {Corrected/re-uploaded package: what changed, who is affected, what must be downloaded/replaced, and how the source can be identified. Omit for ordinary releases.}

{One or two sentences: what this version delivers and why it matters in the user's work. State the comparison baseline where useful.}

## ✨ Highlights

### {Meaningful user outcome}

{What changed, what someone can now do, and why it matters.}

{Optional actual screenshot or demo with version-appropriate immutable URL, alt text and a caption identifying fixture/concept/runtime evidence. Omit if no accurate media exists.}

## Fixes & improvements

- **{Area}:** {Concrete trigger and corrected behavior. Distinguish product fixes from test-only corrections.}

## 📦 Download

| Package | Recommended for |
|---|---|
| [{Actual package name}]({Verified asset URL}) | {Relevant choice and launch prerequisites} |

**Requirements:** {Verified architecture, OS and runtime; bundled runtime does not extend OS support.}

**Security:** {Actual signing status; verified checksums/manifest links and source identity.}

## 🔄 Updating

1. {Data to back up.}
2. {How to fully stop the existing application.}
3. {Verified installer/file replacement/migration steps and any required consent.}

**Your data:** {Locations, preserved originals, schema changes, migration conflicts and downgrade limits. Never infer complete rollback.}

## Known limitations

- **{Area}:** {Specific unsupported state, risk or missing acceptance. Keep material restrictions visible and link the exact detailed contract.}

## Verification & technical details

{Short statement of checks actually observed on this release source. Separate unit/component/hosted/native evidence; do not imply prose changes reran product tests.}

- [Full changelog]({Verified version-specific URL})
- [Compare changes]({Verified published-baseline comparison; omit for initial release})
- [Test evidence]({Existing immutable evidence matching the source})
- [Report an issue]({Only if a functioning reporting destination is verified; otherwise omit and state the gap})

## License & credits

{Verified license, upstream attribution and required notices.}
```

Before owner review, retrieve live metadata/bodies/assets, preserve originals and verify the facts against each version. Check actual package names and checksum/manifest identities, every URL/image, existing issue-reporting configuration and wide/narrow Markdown rendering. Recheck every release against its original body and source for dropped warnings or newer features accidentally attributed to an older version.

Separate **drafted**, **validated**, **approved** and **published**. Editing the files does not authorize live release updates. With explicit approval, update only the requested descriptions; verify body equality and unchanged tags/assets/channel/publication dates afterward. Do not create releases, rebuild packages or change repository settings as part of editorial work.
