# Xbox test build

The `xbox-uwp` branch contains the Xbox Developer Mode UWP client for Bin Weevils Rewritten.

## Confirmed hardware result — 14 July 2026

The first Xbox test established that:

- the signed x64 package installs through Xbox Device Portal;
- the application launches from Dev Home;
- `https://play.binweevils.app/` renders inside the UWP WebView;
- the main site, news page, login page and registration interface render;
- controller focus is visible and can activate website controls;
- the live website displays its own application-only account-creation message when registration is attempted;
- the initial build renders at an unsuitable desktop scale on a television, cropping portions of the page.

The UWP/WebView approach is therefore viable. Current work is focused on television-safe scaling, spatial controller navigation, forms, login/session persistence and the game/Ruffle bootstrap boundary.

## Download the automated build

1. Open the repository on GitHub.
2. Select **Actions**.
3. Open the latest successful **Build Xbox UWP** run for the `xbox-uwp` branch.
4. Download the numbered `Bin-Weevils-Rewritten-Xbox-test-<run>` artifact.
5. Extract the ZIP on the Windows PC used to access Xbox Device Portal.

The artifact contains a signed test `.msix`, its public `.cer` certificate, installation scripts and generated UWP dependencies. The private signing key is generated temporarily during the workflow and is not uploaded.

## Install through Xbox Device Portal

1. Boot the Xbox into Developer Mode.
2. Open the Xbox Device Portal address shown in Dev Home.
3. Under **My games & apps**, choose **Add**.
4. Select `BinWeevilsRewrittenXbox_0.1.0.0_x64.msix` as the application package.
5. If Device Portal requests dependencies, add only the three files under `Dependencies\x64`:
   - Microsoft.NET.Native Framework 2.2;
   - Microsoft.NET.Native Runtime 2.2;
   - Microsoft.VCLibs x64 14.00.
6. Start the installation.
7. Launch **Bin Weevils Rewritten Xbox** from Dev Home.

The `.cer` is supplied for Windows-side testing and inspection. Xbox Device Portal normally installs the signed MSIX directly.

## Current controller map

- D-pad or left stick: move focus spatially between visible controls;
- A: activate the focused control or enter a text field;
- B: browser back;
- X: refresh;
- Y: return to the Bin Weevils home page;
- right stick: smooth vertical page scrolling;
- LB/RB: decrease/increase page zoom in 5% steps;
- View: reset page zoom to 78%;
- Menu: toggle the controller-help overlay.

The selected zoom value is saved in UWP LocalSettings and restored after restarting the application.

## TV layout test checklist

- [ ] The whole login form is visible without the right or bottom edge being cut off.
- [ ] The site remains centred within the 24-pixel television-safe border.
- [ ] Default 78% zoom is readable at normal viewing distance.
- [ ] LB/RB changes zoom and displays the temporary zoom indicator.
- [ ] View resets zoom to 78%.
- [ ] The selected zoom survives closing and reopening the app.
- [ ] News/blog cards fit within the visible screen width.
- [ ] Website modal dialogs remain centred and their confirmation button is reachable.
- [ ] Focus movement follows the visual direction of controls rather than raw DOM order.
- [ ] Focusing an off-screen field scrolls it into the centre of the display.

## Site and account test checklist

- [x] The app reaches `https://play.binweevils.app/`.
- [x] The login and registration pages render.
- [x] Links to the main site and news/blog remain inside the app.
- [ ] External links are handed to the system browser.
- [ ] Keyboard input works in username and password fields.
- [ ] Mouse clicks and scrolling work.
- [x] Xbox controller focus and A-button activation work on ordinary page controls.
- [ ] Closing and reopening the app preserves the web session when service cookies permit it.
- [ ] A valid existing account can authenticate.
- [ ] The Play flow reaches the current game/Ruffle bootstrap boundary.
- [ ] Record the exact screen, URL and error if entering the game fails.

## Known website behaviour

Attempting to create a new account currently produces a website message stating that its application must be used. This is not a UWP installation error. It appears to be website-side client detection or an intentionally restricted registration route. Existing-account login should be tested independently before changing or emulating that detection.

## Game bootstrap limitation

The client does not currently bundle a separate Ruffle runtime. Testing the Play flow will determine whether the official site supplies a compatible Ruffle/WebAssembly bootstrap inside UWP WebView or whether a local packaged player and controlled asset bridge are needed.

## Local build

Install Visual Studio 2022 with:

- Universal Windows Platform development;
- Windows 10 SDK 10.0.19041 or newer;
- .NET UWP tooling.

Then run:

```powershell
.\uwp\scripts\Generate-PlaceholderAssets.ps1
.\uwp\scripts\Package-Xbox.ps1 -Configuration Release -Platform x64
```
