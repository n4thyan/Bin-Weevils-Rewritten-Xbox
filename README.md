# Bin Weevils Rewritten Xbox

An experimental Xbox Developer Mode and Windows UWP client for **Bin Weevils Rewritten**.

This repository began as a fork of the original Electron desktop client. The Xbox port replaces the old Electron and Pepper Flash shell with a native UWP application that opens the official Bin Weevils Rewritten web service in a full-screen Xbox-friendly client.

> [!IMPORTANT]
> This project is an independent compatibility experiment. It is not an official Xbox, Microsoft, Bin Weevils, or Bin Weevils Rewritten release. The client does not bundle the game, SWF files, accounts, or server assets.

## Current status

**First Xbox test build in progress.**

The UWP client currently includes:

- Full-screen navigation to `https://play.binweevils.app/`
- Support for the official `binweevils.app` domain and its subdomains only
- Persistent WebView cookies and login sessions
- Internal handling for login, registration, blog/news, and game pages
- External-link protection so unrelated websites open outside the client
- Xbox controller **B/View**, Escape, and browser-back navigation
- Loading and network-error overlays
- x64 APPX packaging for Xbox Series S/X Developer Mode
- Automated GitHub Actions test builds with temporary sideload certificates

The website shell is ready for its first hardware test. Full gameplay still depends on validating the live game's Ruffle/bootstrap path inside Xbox UWP WebView.

## Project layout

```text
.
├── main.js / preload.js       Original Electron reference client
├── uwp/
│   ├── Bin-Weevils-Rewritten-Xbox.sln
│   ├── BinWeevilsRewrittenXbox/
│   │   ├── App.xaml
│   │   ├── MainPage.xaml
│   │   ├── Package.appxmanifest
│   │   └── Properties/
│   ├── scripts/
│   │   ├── Generate-PlaceholderAssets.ps1
│   │   └── Package-Xbox.ps1
│   ├── README.md
│   └── TESTING.md
└── .github/workflows/build-xbox-uwp.yml
```

## Downloading a test build

1. Open the repository's **Actions** tab.
2. Select **Build Xbox UWP**.
3. Open the latest successful run.
4. Download the `Bin-Weevils-Rewritten-Xbox-test` artifact.
5. Extract the ZIP before installing it.

The test artifact should contain the signed `.appx` package and its matching `.cer` certificate.

## Installing on Xbox Developer Mode

1. Boot the Xbox into Developer Mode.
2. Enable Xbox Device Portal in **Remote Access Settings**.
3. Open the Device Portal address from a PC on the same network.
4. Install the certificate from the artifact if the portal requests it.
5. Click **Add**, select the `.appx`, and complete installation.
6. Launch **Bin Weevils Rewritten Xbox** from Dev Home.

Detailed test steps and expected behaviour are documented in [`uwp/TESTING.md`](uwp/TESTING.md).

## Building locally

Requirements:

- Windows 10 or Windows 11
- Visual Studio 2022
- **Universal Windows Platform development** workload
- Windows 10 SDK 10.0.19041 or compatible
- PowerShell 5.1 or newer

Generate temporary assets and build:

```powershell
cd uwp
.\scripts\Generate-PlaceholderAssets.ps1
.\scripts\Package-Xbox.ps1 -Configuration Release -Platform x64
```

Build output is written under `uwp/AppPackages/`.

## Testing priorities

The first Series S/X test should confirm:

1. The APPX installs and opens.
2. The official website renders correctly.
3. Blog, login, and registration pages are usable.
4. Cookies survive an app restart.
5. Keyboard and mouse work in forms.
6. Xbox controller back navigation works.
7. The game launch page exposes or loads its Ruffle player.
8. Any Ruffle, WebAssembly, CORS, cookie, or mixed-content failures are recorded.

## Roadmap

- [x] Preserve the upstream Electron client as a reference
- [x] Add an Xbox-compatible UWP solution
- [x] Load official Bin Weevils Rewritten pages
- [x] Restrict in-app navigation to `binweevils.app`
- [x] Add login-session persistence and safe navigation
- [x] Add automated signed APPX builds
- [ ] Complete first Xbox Series S/X hardware test
- [ ] Identify the live game bootstrap and SWF/Ruffle behaviour
- [ ] Add a local Ruffle host if the website bootstrap cannot run directly
- [ ] Add controller-driven cursor support for mouse-only game interfaces
- [ ] Add Xbox-safe on-screen keyboard and chat flow
- [ ] Replace placeholder package artwork
- [ ] Produce a reproducible pre-release package

## Acknowledgements

The original desktop client and its contributors made this compatibility experiment possible.

### Special shoutout

> **TODO:** Add the full personal shoutout message here.
>
> Suggested format: **Huge shoutout to [NAME] — [YOUR MESSAGE].**

## Legal and attribution

The upstream client declares the **CC0-1.0** licence in `package.json`. Existing upstream attribution has been retained in the repository history.

This fork does not grant rights to Bin Weevils artwork, trademarks, hosted game files, user accounts, or server content. Those remain the property of their respective owners. Do not package or redistribute proprietary game assets without permission.
