# IntuneWin Packager

A Windows 11 Fluent Design GUI for Microsoft's [IntuneWinAppUtil.exe](https://github.com/microsoft/Microsoft-Win32-Content-Prep-Tool) (Win32 Content Prep Tool). Package your Win32 apps as `.intunewin` files for Microsoft Intune without touching the command line.

## Features

- Browse or drag-and-drop source folders, setup files, and output paths
- Real-time output log with copy and clear buttons
- Auto-download IntuneWinAppUtil.exe from GitHub (or point to your own)
- Recent paths history for source and output folders
- Setup file location warning (catches the common "file not in source folder" mistake)
- Open output folder after packaging completes
- Quiet / Super Quiet mode toggles
- Remembers your settings, window size, and position between sessions
- Native Windows 11 look with Mica backdrop and system theme support

## Download

Grab the latest release from the [Releases](https://github.com/liwo-1/IntuneWinAppUtilGUI/releases) page. The self-contained exe runs on Windows 10/11 x64 with no dependencies.

## Build from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build
dotnet run --project IntuneWinPackager
```

### Publish a self-contained exe

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true
```

## Tech Stack

- C# / .NET 8 / WPF
- [WPF UI (Lepoco)](https://github.com/lepoco/wpfui) for Fluent Design
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) for MVVM

## License

[MIT](LICENSE)
