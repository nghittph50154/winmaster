# WinMaster

<div align="center">
  <h1>🔧 WinMaster v1.0</h1>
  <p><strong>Windows Utility & Software Installer</strong></p>
  <p><em>TICK → RUN → DONE</em></p>
  
  ![Version](https://img.shields.io/badge/version-1.0.0-6C63FF?style=flat-square)
  ![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
  ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)
  ![WPF](https://img.shields.io/badge/UI-WPF-0078D4?style=flat-square)
</div>

---

## What is WinMaster?

WinMaster is a personal Windows software installer and utility tool. It lets you set up a fresh Windows installation in minutes — just tick what you need and hit RUN.

**Philosophy:** The user should never need to understand *how* an app is installed — only *that* it's installed. All backend complexity (winget, direct downloads, PowerShell, verification) is hidden behind a clean, modern interface.

---

## Features

- **58 curated applications** across 10+ categories
- **Dark premium UI** — wide, modern, application cards
- **Search** — filter apps by name instantly
- **Select All / Clear** — batch selection
- **Sequential installation** — apps install one by one, failures don't block others
- **Post-install verification** — confirms the app actually installed
- **Fixed version support** — pin specific versions (Python 3.12.8, CapCut 1.5/7.0/7.7, drivers)
- **Archive handling** — ZIP/7Z/RAR files are saved to `Downloads/`, not auto-run
- **Internet connectivity check** — before downloading
- **Admin privilege handling** — detects and requests UAC when needed
- **Real-time log panel** — see exactly what's happening
- **3 menu slots** — Install (full), Tweaks (v1.1), System Tools (v1.2)
- **Remote installer** — `irm <url> | iex` support

---

## Screenshots

> Coming soon — build and run WinMaster to see the UI.

---

## Architecture

```
WinMaster/
├── src/WinMaster/
│   ├── Models/          ← ApplicationEntry, InstallResult, LogEntry
│   ├── ViewModels/      ← MainViewModel, InstallViewModel, ApplicationCardViewModel
│   ├── Views/           ← MainWindow, InstallView, PlaceholderView
│   ├── Services/        ← AppRegistryService, InstallerEngine, VerificationService, ...
│   ├── Installers/      ← WingetInstaller, DirectInstaller, FixedSourceInstaller, ...
│   ├── Infrastructure/  ← PowerShellRunner, FileSystemHelper
│   ├── Converters/      ← WPF value converters
│   └── Themes/          ← Colors.xaml, Controls.xaml
│
├── config/
│   └── applications.json   ← Application registry (the single source of truth)
│
├── scripts/
│   ├── install.ps1         ← Remote entry point (irm | iex)
│   ├── applications/       ← Per-app PowerShell installers
│   ├── utilities/          ← Helper scripts
│   └── system/             ← System setup scripts
│
├── sources/
│   ├── python/             ← Fixed Python installer (user provided)
│   ├── capcut/             ← Fixed CapCut versions (user provided)
│   └── drivers/            ← Fixed driver packages (user provided)
│
└── docs/                   ← Documentation
```

---

## Requirements

- **OS:** Windows 10 (1809+) or Windows 11
- **Runtime:** .NET 10 (included in published self-contained build)
- **winget:** Windows Package Manager (pre-installed on Windows 11, available for Win10)
- **Internet:** Required for most app installations

---

## Installation

### Option 1: Direct Download
Download `WinMaster.exe` from the [Releases](https://github.com/YOUR_USERNAME/WinMaster/releases) page and run it.

### Option 2: Remote PowerShell (irm | iex)
```powershell
irm https://raw.githubusercontent.com/YOUR_USERNAME/WinMaster/main/scripts/install.ps1 | iex
```

> ⚠️ **Security note:** Only run this from the official WinMaster repository. Never pipe unknown scripts to `iex`.

---

## Development Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows (WPF requires Windows)
- Visual Studio 2022 or VS Code with C# extension

### Clone and Build
```bash
git clone https://github.com/YOUR_USERNAME/WinMaster.git
cd WinMaster
dotnet build src/WinMaster/WinMaster.csproj
```

### Run in Development
```bash
dotnet run --project src/WinMaster/WinMaster.csproj
```

---

## Build

### Debug Build
```bash
dotnet build src/WinMaster/WinMaster.csproj
```

### Release Build (Single EXE)
```bash
dotnet publish src/WinMaster/WinMaster.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

---

## PowerShell Scripts

### Organization
```
scripts/
├── install.ps1                  ← Remote installer entry point
├── applications/
│   ├── install-codex.ps1        ← OpenAI Codex CLI (via npm)
│   └── ...
├── utilities/
│   └── ...
└── system/
    └── ...
```

### Remote Entry Point
The `scripts/install.ps1` file is the entry point for remote installation:
```powershell
irm https://raw.githubusercontent.com/YOUR_USERNAME/WinMaster/main/scripts/install.ps1 | iex
```

To change the remote URL, update `remoteBaseUrl` in `config/applications.json`.

---

## How to Use

1. Launch `WinMaster.exe`
2. Click **INSTALL** (default menu)
3. Browse categories or use the **Search** bar
4. **Tick** the applications you want
5. Click **RUN INSTALLATION**
6. Watch the log panel — WinMaster handles everything
7. Review the summary when complete

---

## How to Add an Application

Edit `config/applications.json` and add an entry:

```json
{
  "id": "my-app",
  "name": "My App",
  "displayName": "My App",
  "category": "Utilities",
  "description": "Description of my app",
  "tags": ["tag1", "tag2"],
  "installer": {
    "type": "winget",
    "wingetId": "Publisher.MyApp"
  },
  "verification": {
    "strategies": ["command"],
    "command": "myapp --version"
  },
  "requiresAdmin": false,
  "versionPolicy": "latest",
  "status": "stable"
}
```

**Installer types:**
| Type | Description |
|------|-------------|
| `winget` | Windows Package Manager (recommended) |
| `direct` | Direct download from official URL |
| `fixed` | From `sources/` directory |
| `powershell` | Custom PowerShell script |
| `store` | Microsoft Store |
| `pending` | Not yet configured |

---

## How to Add a Fixed Source

1. Add your file to the appropriate `sources/` subdirectory:
   ```
   sources/
   ├── python/         ← Python-3.12.8.exe
   ├── capcut/         ← CapCut-1.5.exe, CapCut-7.0.exe, ...
   └── drivers/        ← YINDIAO-G17-Drive.zip, ...
   ```

2. Add an entry to `applications.json` with `"type": "fixed"`:
   ```json
   "installer": {
     "type": "fixed",
     "sourceDir": "sources/python",
     "expectedPattern": "Python-3.12.8"
   }
   ```

3. WinMaster will find the file by pattern match — **file extension is detected at runtime**, never assumed.

> **Important:** If the source file is not found, WinMaster shows a clear error. It will NOT substitute a different version.

---

## Security Notes

- WinMaster only executes scripts from within its own repository
- `irm | iex` downloads from the **official WinMaster GitHub repository** only
- ExecutionPolicy is set per-session (`-ExecutionPolicy Bypass -Scope Process`), never globally modified
- No telemetry, no phone home
- WinMaster does NOT modify Windows Defender or security settings
- Admin privileges are only requested when the application requires them

---

## Version

**Current:** `1.0.0`

Version is managed in a single location:
- `src/WinMaster/WinMaster.csproj` → `<Version>1.0.0</Version>`

---

## Roadmap

| Version | Features |
|---------|----------|
| **v1.0** | Install menu, 58 apps, sequential install, verification, fixed sources ✅ |
| **v1.1** | Tweaks menu, Windows optimization, startup management |
| **v1.2** | System Tools menu, disk cleanup, driver management |
| **v2.0** | Uninstall manager, update manager, installation history |

---

## License

MIT License — see [LICENSE](LICENSE) file.

---

<div align="center">
  <p>Built with ❤️ for power users who value simplicity</p>
  <p><strong>WinMaster</strong> — TICK → RUN → DONE</p>
</div>
