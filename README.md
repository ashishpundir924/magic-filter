# DEFOR Live Filter for Revit 2023

Advanced Revit add-in for selection-based live filtering with category, family, type, and multi-rule parameter logic.

## Repo structure

- `MagicDefor.Revit/` - main Revit add-in source
- `MagicDefor.Installer/` - Windows installer/uninstaller source
- `build/MagicDefor.Revit/` - canonical repo build output used by the Revit manifest
- `release/installer/` - shareable installer output
- `MagicDefor.slnx` - solution file

## Main features

- `DEFOR Tools` ribbon integration in Revit 2023
- modeless live-filter window
- source scope tree with category, family, and type selection
- search, select all, clear all, expand all, and collapse all in source scope
- mixed `AND` / `OR` logic between filter rules
- saved configurations
- temporary isolate, select, apply, and clear actions
- installer with uninstall support through Windows Apps & Features

## Build

1. Open `MagicDefor.slnx` in Visual Studio 2022 or newer.
2. Build `MagicDefor.Revit` for the add-in DLL.
3. Build or publish `MagicDefor.Installer` for the installer EXE.

## Runtime path

The Revit add-in manifest points to this single runtime DLL:

`build\MagicDefor.Revit\MagicDefor.Revit.dll`

That is the canonical DLL path to use for testing and release.

## Installer

The shareable installer output is here:

`release\installer\MagicDefor.Installer.exe`

Install behavior:

- copies the add-in to `%LocalAppData%\Programs\DEFOR Live Filter\Revit 2023`
- writes the Revit `.addin` manifest for the current user
- registers uninstall information in Windows Apps & Features

## Revit usage

1. Start Revit 2023.
2. Open any model and select source elements.
3. Go to `DEFOR Tools`.
4. Open `Live Filter`.
5. Click `Read Selection`.

## Notes

- Revit locks the active add-in DLL while it is open.
- If you rebuild the add-in, close Revit first, then build, then reopen Revit.
- The lightweight installer EXE in this repo is framework-dependent.