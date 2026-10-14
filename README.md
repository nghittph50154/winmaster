# WinMaster

<div align="center">
  <h1>🔧 WinMaster v1.1.5</h1>
  <p><strong>Windows Utility & Software Installer</strong></p>
  <p><em>TICK → RUN → DONE</em></p>
  
  ![Version](https://img.shields.io/badge/version-1.1.5-6C63FF?style=flat-square)
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

- **60+ curated applications** across 10+ categories (Browsers, Communications, Development, Document, Games, Utilities, Media, Drivers, etc.)
- **3 Execution Modes** (Single Selection):
  - ⚡ **Cài tự động (Silent)**: Tự động tải và cài đặt im lặng vào máy
  - 🖥️ **Hiện Setup (Interactive)**: Hiện giao diện wizard của phần mềm để tùy biến
  - 💾 **Chỉ tải file thôi (Download Only)**: Chỉ tải file cài đặt `.exe`/`.msi` về máy mà không cài
- **Custom Download Location**: Tùy chọn thư mục lưu trữ file cài đặt riêng biệt
- **PowerShell Activity Log Window**: Cửa sổ console PowerShell riêng biệt hiển thị log chi tiết thời gian thực với màu sắc chuẩn trực quan
- **WinUtil-style Status & Progress Bar**: Thanh tiến độ tinh gọn và thông báo tóm tắt kết quả ở thanh đáy
- **Auto-clean Manifest Files**: Tự động dọn dẹp các file `.yaml` rác sau khi tải gói winget
- **Dark premium UI** — giao diện rộng tràn viền, hiện đại, mượt mà
- **Search & Filter** — tìm kiếm ứng dụng tức thì
- **Select All / Clear** — thao tác hàng loạt
- **Sequential installation** — cài đặt tuần tự an toàn, lỗi không chặn các ứng dụng khác
- **Tweaks Menu (v1.1)** — tối ưu hóa Windows, gỡ bỏ Bloatware, tinh chỉnh giao diện, kích hoạt bản quyền Windows / Office / IDM
- **WinPE Menu (dự kiến v1.2)** — bộ công cụ cứu hộ và cài lại máy
- **One-click Launcher** — chạy trực tiếp qua PowerShell: `powershell -ExecutionPolicy Bypass -File .\run.ps1`

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
irm bit.ly/phongvntv | iex
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

**Current:** `1.1.5`

Version is managed in a single location:
- `src/WinMaster/WinMaster.csproj` → `<Version>1.1.5</Version>`

---

## Roadmap

| Version | Features |
|---------|----------|
| **v1.0** | Install menu, sequential install, verification, fixed sources ✅ |
| **v1.1** | Tweaks menu, Windows optimization, remove bloatware, activate Windows/Office/IDM ✅ |
| **v1.1.5** | 3 chế độ cài đặt, PowerShell Live Log, WinUtil progress bar, dọn dẹp file YAML, tối ưu 13 ứng dụng chính thức ✅ |
| **v1.2** | WinPE menu, công cụ cứu hộ và triển khai hệ điều hành |
| **v2.0** | Quản lý gỡ cài đặt, kiểm tra cập nhật ứng dụng tự động |

---

## License

MIT License — see [LICENSE](LICENSE) file.

---

<div align="center">
  <p>Built with ❤️ for power users who value simplicity</p>
  <p><strong>WinMaster</strong> — TICK → RUN → DONE</p>
</div>
