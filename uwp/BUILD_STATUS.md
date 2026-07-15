# Xbox UWP Build Status

## Current remote-build baseline

- Repository: `n4thyan/Bin-Weevils-Rewritten-Xbox`
- Branch: `xbox-uwp`
- Target: Xbox Series S/X Developer Mode
- Configuration: Release x64 UWP
- Packaging: signed sideload APPX

## Confirmed

- NuGet/package restore completes on the GitHub-hosted Windows runner.
- The UWP application reaches the signed APPX build stage.
- The first packaging failure was caused by an invalid `AppListEntry="all"` manifest value.
- The manifest now uses the supported `AppListEntry="default"` value.

## Active milestone

Produce a green GitHub Actions run containing:

- the installable APPX package;
- its matching temporary test certificate;
- any generated dependency packages;
- the Xbox testing checklist;
- separate restore/build diagnostic logs.

Do not treat the web client as gameplay-validated until it has been installed on Xbox hardware and the live Ruffle/bootstrap path has been observed.
