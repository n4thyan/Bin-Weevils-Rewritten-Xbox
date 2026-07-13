# Xbox test build

The `xbox-uwp` branch contains the first Xbox UWP web client for Bin Weevils Rewritten.

## Download the automated build

1. Open the repository on GitHub.
2. Select **Actions**.
3. Open the latest **Build Xbox UWP** run for the `xbox-uwp` branch.
4. Download the `Bin-Weevils-Rewritten-Xbox-test` artifact.
5. Extract the ZIP on the Windows PC used to access Xbox Device Portal.

The artifact contains a signed test `.appx` and its public `.cer` certificate. The private signing key is generated temporarily during the workflow and is not uploaded.

## Install through Xbox Device Portal

1. Boot the Xbox into Developer Mode.
2. Open the Xbox Device Portal address shown in Dev Home.
3. Under **My games & apps**, choose **Add**.
4. Select the generated `.appx` as the application package.
5. Add dependency packages from the generated package folder if Device Portal requests them.
6. Start the installation.
7. Launch **Bin Weevils Rewritten Xbox** from Dev Home.

The `.cer` is supplied for Windows-side testing and inspection. Xbox Device Portal normally installs the signed APPX directly.

## First test checklist

- The app reaches `https://play.binweevils.app/`.
- The login and registration pages render.
- Links to the main site and news/blog remain inside the app.
- External links are handed to the system browser.
- Keyboard input works in text fields.
- Mouse clicks and scrolling work.
- Xbox **B**, browser-back, or Escape returns to the previous page.
- Closing and reopening the app preserves the web session when the service cookies permit it.
- Record the exact screen, URL, and error shown when attempting to enter the game.

## Expected limitation

This build is a site-shell milestone. It does not yet bundle Ruffle. The result of launching the game will determine whether the official site already supplies a compatible Ruffle bootstrap or whether the client needs its own local Ruffle host.

## Local build

Install Visual Studio 2022 with:

- Universal Windows Platform development
- Windows 10 SDK 10.0.19041 or newer
- .NET UWP tooling

Then run:

```powershell
.\uwp\scripts\Generate-PlaceholderAssets.ps1
.\uwp\scripts\Package-Xbox.ps1 -Configuration Release -Platform x64
```
