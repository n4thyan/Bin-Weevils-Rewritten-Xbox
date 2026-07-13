# Bin Weevils Rewritten Xbox UWP Port

This branch replaces the legacy Electron and Pepper Flash shell with an Xbox-compatible UWP client that uses Ruffle.

## Initial target

- Xbox Series S and Series X in Developer Mode
- UWP package installable through Xbox Device Portal
- Full-screen 1920x1080 client
- Ruffle-based Flash playback
- Keyboard and mouse support first
- Xbox controller cursor support after the basic client loads
- Remote content loaded from the official Bin Weevils Rewritten service
- No bundled proprietary game assets

## Architecture

```text
Xbox UWP application
  -> WebView host
  -> local HTML bootstrap
  -> bundled Ruffle runtime
  -> official remote game/bootstrap endpoints
```

## Phase 1: proof of concept

1. Create a minimal UWP application shell.
2. Add a full-screen WebView.
3. Load a local diagnostic page.
4. Verify JavaScript, WebAssembly, audio, keyboard and mouse input on Xbox.
5. Add Ruffle and load a harmless public test SWF.
6. Package as APPX and test through Xbox Device Portal.

## Phase 2: Bin Weevils bootstrap

1. Inspect the official web client and identify its current game bootstrap flow.
2. Reproduce required URL parameters, cookies and network requests.
3. Load the game through the local Ruffle host.
4. Preserve authentication without storing credentials in source control.
5. Add useful diagnostics for failed resource and socket requests.

## Phase 3: Xbox controls

- Left stick: cursor movement
- A: primary click
- B: back / Escape
- D-pad: menu navigation
- Menu: settings
- USB keyboard: login and chat

## Removed Electron features

- Pepper Flash plugin loading
- Electron updater
- Discord Rich Presence
- Electron preload and IPC
- Desktop global shortcuts
- Desktop window and menu management

## Development rule

Keep `main` as the original Electron reference. All UWP work starts on `xbox-uwp` until the first Xbox package launches successfully.
