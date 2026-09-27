# Milky Frog

Windows desktop pet application built with .NET 8 and WPF.

## Project layout

```text
src/
  MilkyFrog.Core/              Platform-independent animation and service contracts
  MilkyFrog.Platform.Windows/  Windows tray, audio, cursor, and single-instance services
  MilkyFrog.App/               WPF composition root, window, animation presenter, and assets
tests/
  MilkyFrog.Core.Tests/        Unit tests for platform-independent behavior
tools/
  GeneratePixelFrogAssets.ps1  Windows entry point for video-to-PNG asset generation
  generate_video_pixel_frog.py Image extraction and transparent pixel processing
  create_icon_from_image.py   Transparent Windows icon generation
```

Runtime assets live under `src/MilkyFrog.App/Assets`. `Composition/AssetCatalog.cs`
contains the mapping from animation states to those files; it is intentionally
separate from the asset files themselves.

## Build and test

```powershell
dotnet restore
dotnet test
dotnet build
dotnet run --project .\src\MilkyFrog.App\MilkyFrog.App.csproj
```

The application targets `net8.0-windows` and `x64` because it uses WPF and
Windows audio/tray APIs.

## Regenerate character frames

Install the packages listed in `tools/requirements-assets.txt`, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\GeneratePixelFrogAssets.ps1 `
  -VideoPath "E:\path\to\Naiwa.mp4"
```

The source and licensing notes for the character frames are in
`src/MilkyFrog.App/Assets/Character/SOURCE.md`.
