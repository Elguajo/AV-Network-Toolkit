# Contributing

Bug reports, vendor subnet corrections and pull requests are all welcome.

## Before you open an issue

- Questions and ideas go to [Discussions](https://github.com/rmzncakir/network-profile-changer/discussions).
- Security problems must **not** go into a public issue — see [SECURITY.md](SECURITY.md).
- Leave real site addresses out. Example addresses such as `192.168.0.x` are enough to reproduce almost every problem.

## Pull requests

1. Add tests under `test/` for new logic.
2. `dotnet build` and `dotnet test test/NetworkProfileManager.Tests.csproj` must pass.
3. New UI text goes into `app/Strings/En.xaml` (English is the supported UI language); resource tests check for missing and duplicate keys.
4. Point out anything security-relevant (elevation, `netsh` arguments, file paths) in the PR description.
5. Add a line under `## [Unreleased]` in [CHANGELOG.md](CHANGELOG.md) for user-visible changes.

## Screenshots

Debug builds accept `--demo Light` or `--demo Dark`. The app then starts with invented adapters, profiles, history and
scan results, and keeps its data in a temporary folder, so a screenshot never shows your real network. The images in
`docs/screenshots/` were taken this way.
