# Bin Weevils Rewritten Xbox UWP

This folder contains the experimental Xbox Dev Mode client.

## Current milestone

The first build is a full-screen UWP browser shell for the official Bin Weevils Rewritten service. It starts at `https://play.binweevils.app/`, retains the UWP WebView cookie/session store, keeps Bin Weevils links inside the app, and sends unrelated links to the system browser.

## Requirements

- Windows 10 or Windows 11
- Visual Studio 2022
- Universal Windows Platform development workload
- Windows 10 SDK 10.0.19041.0 or newer
- Xbox Series S/X in Developer Mode

## Build

1. Open `Bin-Weevils-Rewritten-Xbox.sln`.
2. Select `Release` and `x64`.
3. Right-click the project and choose **Publish > Create App Packages**.
4. Choose **Sideloading** and disable Microsoft Store upload.
5. Create or select a local test certificate when Visual Studio asks.
6. Build the package.
7. Upload the generated `.appx` or `.msixbundle` and dependencies using Xbox Device Portal.

## First Xbox test

Verify that:

- the login page appears;
- the main website, blog/news, registration and login links work;
- keyboard text entry works;
- mouse clicking and scrolling work;
- authentication survives navigation;
- launching the game exposes the current Flash/Ruffle failure point.

## Limitations

The first milestone does not yet bundle Ruffle. It establishes the official site, authentication and navigation path first. Ruffle integration will be implemented after testing the live game bootstrap inside Xbox UWP WebView.
