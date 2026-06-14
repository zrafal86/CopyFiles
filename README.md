# CopyFiles

A cross-platform desktop application for copying files from a source folder to a destination folder, using MD5 hash verification to deduplicate files. Built with Avalonia UI and .NET 10.

## Features

- Select source and destination folders via a native folder picker
- MD5 hash-based file deduplication — files already present at the destination (by hash) are skipped
- Two copy strategies to compare performance:
  - **One Thread** — sequential hash calculation and file copy
  - **Multi Thread** — parallel hash calculation and file copy using `Task.Factory`
- Progress bar showing real-time copy progress
- Timing results for both hash and copy phases in each mode
- Side-by-side speed comparison once both modes have been run

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build & Run

```bash
dotnet run --project BlankCoreAppCopyTask
```

## Usage

1. Click **Select source** and choose the folder containing files to copy.
2. Click **Select destination** and choose the target folder.
3. Select a copy method: **OneThread** or **MultiThread**.
4. Click **Copy** to start. The progress bar updates as files are transferred.
5. Run again with the other method to see the speed comparison in the **Result** section.
6. Use **Clear dest folder** to delete all files in the destination before re-running a test.

## How It Works

Before copying, the app calculates an MD5 hash of each source file. The destination filename is set to `<hash><extension>`, so duplicate files are detected and skipped automatically. Only files not already present at the destination are copied.

### Copy Strategies

| Strategy | Hash Phase | Copy Phase |
|---|---|---|
| OneThread | Sequential | Sequential |
| MultiThread | Parallel (`Task.Factory`) | Parallel (`Task.WhenAll`) |

After running both strategies the UI shows how much faster or slower the multi-thread approach was for each phase.

## Generating Test Files (Windows)

```cmd
mkdir c:\temp\src
mkdir c:\temp\dst
fsutil file createnew c:\temp\src\1gb.test 1073741824
```

Common sizes:

| Size | Bytes |
|---|---|
| 1 MB | 1 048 576 |
| 100 MB | 104 857 600 |
| 1 GB | 1 073 741 824 |
| 10 GB | 10 737 418 240 |

## Tech Stack

- [Avalonia UI 11](https://avaloniaui.net/) — cross-platform UI framework
- [CommunityToolkit.Mvvm 8](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — MVVM helpers (`ObservableObject`, relay commands)
- [Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection) — DI container
- .NET 10 / C#
