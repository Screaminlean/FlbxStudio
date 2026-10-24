# FlbxStudio

FlbxStudio is a WPF desktop tool for opening, inspecting, and editing Weintek `.flbx` picture library files.

Current baseline: **beta-ready**.

## Current status

- Supports loading FLBX files with catalog-based and inline/trailing asset variations.
- Supports multi-asset libraries such as `Conveyors.flbx`.
- Supports separate Program and User library workflows from dedicated navigation pages.
- Supports importing/removing state images and saving updated libraries.
- Save dialogs now default to the active FLBX filename and folder.
- Settings includes configurable Program/User library root paths.

# Building

## Prerequisites

- .NET 10 SDK
- Windows (WPF desktop target)

## Build (debug)

From repository root:

`dotnet build FlbxStudio/FlbxStudio.csproj -c Debug`

## Publish self-contained EXE (recommended)

`dotnet publish FlbxStudio/FlbxStudio.csproj -c Release -r win-x64 --self-contained true`

Output:

`FlbxStudio/bin/Release/net10.0-windows/win-x64/publish/`

## Publish self-contained single-file EXE

Single-file:

`dotnet publish FlbxStudio/FlbxStudio.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true`

Output:

`FlbxStudio/bin/Release/net10.0-windows/win-x64/publish/`

## Single-file notes

- Startup can be slightly slower than multi-file publish.
- Some native/runtime components may be extracted at runtime.
- Do not enable trimming for this WPF app unless fully validated.