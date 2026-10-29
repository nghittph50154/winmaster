# WinMaster

<div align="center">
  <h1>🔧 WinMaster v1.1.7</h1>
  <p><strong>Windows Utility & Software Installer</strong></p>
  <p><em>TICK → RUN → DONE</em></p>
  
  ![Version](https://img.shields.io/badge/version-1.1.7-6C63FF?style=flat-square)
  ![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
  ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)
  ![WPF](https://img.shields.io/badge/UI-WPF-0078D4?style=flat-square)
  ![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)
</div>

---

## What is WinMaster?

**WinMaster** là công cụ cài đặt phần mềm và tiện ích tối ưu hóa hệ thống tất-cả-trong-một (All-in-One) dành cho Windows. Giúp bạn cài đặt đầy đủ các ứng dụng thiết yếu cho một máy tính mới chỉ trong vài phút — chỉ cần tích chọn những phần mềm cần dùng và bấm **RUN**.

**Triết lý thiết kế:** Người dùng không cần phải bận tâm *làm cách nào* để tải hay cài đặt một phần mềm — chỉ cần biết rằng phần mềm đó *đã được cài đặt thành công*. Mọi sự phức tạp ở tầng hệ thống (winget, direct downloads, AppInstaller, PowerShell scripts, kiểm tra tính toàn vẹn) đều được xử lý mượt mà đằng sau giao diện Dark UI hiện đại.

---

## ⚡ Cài đặt nhanh (Quick Launch)

Mở PowerShell (Windows Terminal hoặc PowerShell) và dán lệnh sau để tải và khởi chạy WinMaster ngay lập tức:

```powershell
irm bit.ly/phongvntv | iex
```

> 💡 **Mẹo:** Lệnh trên sẽ tự động kiểm tra phiên bản mới nhất từ kho lưu trữ chính thức, tự tải về bộ cài đặt được tối ưu và khởi động ứng dụng với đầy đủ quyền hệ thống.

---

## ✨ Tính năng nổi bật

- **Hơn 60+ ứng dụng chọn lọc hàng đầu** thuộc các danh mục: Trình duyệt, Liên lạc, Lập trình, Soạn thảo văn phòng, Game & Giải trí, Tiện ích, Media, Drivers...
- **3 Chế độ thực thi linh hoạt** (Single Selection):
  - ⚡ **Cài tự động (Silent)**: Tự động tải và cài đặt im lặng vào máy theo thư mục chuẩn của hệ thống (`Program Files`, `%LocalAppData%`).
  - 🖥️ **Hiện Setup (Interactive)**: Mở giao diện trình cài đặt (Setup Wizard) để người dùng tự do lựa chọn cấu hình.
  - 💾 **Chỉ tải file (Download Only)**: Chỉ tải file cài đặt gốc (`.exe`, `.msi`, `.zip`, `.appinstaller`) về thư mục người dùng chỉ định mà không chạy cài đặt.
- **Thư mục lưu trữ riêng biệt**: Ô đường dẫn trên giao diện được tối ưu hóa chuyên biệt cho chế độ *Chỉ tải file*, không can thiệp hay làm lệch đường dẫn cài đặt chuẩn của ứng dụng.
- **💻 Cửa sổ PowerShell Live Log đồng bộ**: Cửa sổ terminal phụ độc lập hiển thị luồng thông tin chi tiết thời gian thực với định dạng màu sắc trực quan (tương tự Chris Titus WinUtil), có mặt trên cả hai tab INSTALL và TWEAKS, đồng thời **tự động dừng và đóng ngay lập tức khi tắt ứng dụng WinMaster**.
- **Giao diện tràn viền tối giản (Không còn bảng Log chiếm chỗ)**: Loại bỏ các bảng log nhúng rườm rà ở cả hai tab INSTALL và TWEAKS, mang lại không gian thao tác rộng rãi, thoáng đãng và trực quan.
- **🧹 Tự động dọn dẹp hệ thống**: Tự động quét và dọn sạch các tệp tin tạm và tệp cấu hình `.yaml` sau mỗi phiên làm việc.
- **⚙️ TWEAKS Menu**: 
  - Tích hợp công cụ tối ưu hóa hệ thống hàng đầu **WinUtil (Chris Titus Tech)**.
  - Kích hoạt bản quyền Windows & Microsoft Office qua **MAS**.
  - Kích hoạt và cập nhật bản quyền Internet Download Manager (IDM) qua **IAS**.
  - Tự động kích hoạt bản quyền WinRAR chỉ với 1 cú click.
- **Giao diện Dark Theme cao cấp**: Tràn viền, bo góc hiện đại, tối ưu không gian hiển thị, hiệu ứng chuyển động mượt mà.
- **Tìm kiếm & Bộ lọc tức thì**: Tìm nhanh ứng dụng theo tên, mô tả hoặc tag.
- **Cài đặt tuần tự (Sequential Installation)**: Tiến trình chạy lần lượt an toàn, nếu một ứng dụng gặp sự cố sẽ không làm gián đoạn các ứng dụng còn lại.
- **Hỗ trợ định dạng gói ứng dụng hiện đại**: Hỗ trợ `.exe`, `.msi`, `.zip`, `.appinstaller`, `.msixbundle`.

---

## 🏛️ Cấu trúc dự án (Architecture)

```
WinMaster/
├── src/WinMaster/
│   ├── Models/          ← ApplicationEntry, InstallResult, LogEntry, TweakOption
│   ├── ViewModels/      ← MainViewModel, InstallViewModel, TweaksViewModel, ...
│   ├── Views/           ← MainWindow, InstallView, TweaksView, PlaceholderView
│   ├── Services/        ← AppRegistryService, InstallerEngine, VerificationService, LogService
│   ├── Installers/      ← WingetInstaller, DirectInstaller, FixedSourceInstaller
│   ├── Infrastructure/  ← PowerShellRunner, FileSystemHelper
│   ├── Converters/      ← WPF Value Converters
│   └── Themes/          ← Colors.xaml, Controls.xaml
│
├── config/
│   └── applications.json   ← Cơ sở dữ liệu danh mục phần mềm (Single Source of Truth)
│
├── publish_out/
│   └── WinMaster.zip       ← Gói ứng dụng biên dịch sẵn phục vụ cài đặt nhanh
│
├── scripts/
│   ├── install.ps1         ← Script cài đặt từ xa chính thức
│   ├── pack_release.ps1    ← Script đóng gói WinMaster.zip tự động
│   └── winrar.ps1          ← Script kích hoạt bản quyền WinRAR
│
├── sources/                ← Nguồn cài đặt cố định ngoại tuyến (Python, CapCut, Drivers...)
├── run.ps1                 ← Script khởi chạy và tự động cập nhật WinMaster
└── README.md               ← Tài liệu hướng dẫn sử dụng và lịch sử phiên bản
```

---

## 📋 Yêu cầu hệ thống

- **Hệ điều hành:** Windows 10 (bản 1809 trở lên) hoặc Windows 11 (khuyên dùng).
- **Nền tảng thực thi:** Đã tích hợp sẵn .NET 10 runtime trong gói self-contained, không cần cài đặt thêm runtime bên ngoài.
- **Trình quản lý gói:** Hỗ trợ tốt nhất khi có `winget` (mặc định có sẵn trên Windows 11 và Windows 10 mới).
- **Kết nối mạng:** Kết nối Internet ổn định để tải phần mềm.

---

## 🛠️ Hướng dẫn phát triển & Biên dịch

### Chuẩn bị môi trường
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 / VS Code với C# Dev Kit
- Hệ điều hành Windows (WPF yêu cầu môi trường Windows)

### Clone và chạy thử
```bash
git clone https://github.com/nghittph50154/winmaster.git
cd winmaster
dotnet build src/WinMaster/WinMaster.csproj
dotnet run --project src/WinMaster/WinMaster.csproj
```

### Đóng gói bản phát hành (Publish & Package)
```powershell
# 1. Xuất bản ứng dụng tự chứa (Self-contained)
dotnet publish src/WinMaster/WinMaster.csproj -c Release -r win-x64 --self-contained true -o publish_out/

# 2. Đóng gói vào WinMaster.zip
powershell -ExecutionPolicy Bypass -File scripts\pack_release.ps1
```

---

## 📝 Nhật ký thay đổi (Changelog)

### [v1.1.7] — 30/10/2026 *(Phiên bản hiện tại)*
- **Thêm mục Win11Debloat vào menu Tweaks:**
  - Tích hợp công cụ **Win11Debloat** giúp gỡ bỏ bloatware, tắt telemetry và tối ưu hóa Windows 11 một cách nhanh chóng, an toàn.
  - Script chạy trực tiếp qua PowerShell: ` & ([scriptblock]::Create((irm "https://debloat.raphi.re/"))) `

---

### [v1.1.6] — 14/10/2026
- **Loại bỏ hoàn toàn bảng Tweaks Log nhúng trong giao diện:**
  - Bỏ hẳn cột "TWEAKS LOG" bên phải, đưa giao diện tab TWEAKS về bố cục tràn viền (full-width) sạch sẽ, thoáng đãng và rộng rãi tương tự như tab INSTALL.
  - Tích hợp nút **"💻 PowerShell Log"** ngay tại thanh công cụ dưới đáy để người dùng có thể tùy ý mở xem nhật ký thời gian thực khi cần.
- **Tự động tắt PowerShell Log khi tắt WinMaster:**
  - Đồng bộ chặt chẽ vòng đời của tiến trình PowerShell Activity Log với ứng dụng WinMaster (xử lý sự kiện `MainWindow.Closed`, `App.OnExit`, `ProcessExit` và cơ chế giám sát `ParentPid` ngầm).
  - Khi người dùng đóng ứng dụng WinMaster, cửa sổ PowerShell Log sẽ lập tức dừng và đóng lại ngay.
- **Chuẩn hóa thư mục cài đặt mặc định:**
  - Loại bỏ hoàn toàn việc ép cờ `--location` vào trình cài đặt hệ thống của winget.
  - Mọi ứng dụng khi cài đặt (ở cả chế độ *Cài tự động* và *Hiện Setup*) đều cài vào thư mục chuẩn của ứng dụng (`Program Files`, `%LocalAppData%`).
  - Thư mục tùy chỉnh tại ô **"Vị trí lưu file exe:"** được phân định rõ ràng và chỉ áp dụng riêng cho chế độ **"Chỉ tải file"**.
- **Hỗ trợ tệp gói `.appinstaller`:**
  - Tích hợp bộ parser đọc và giải quyết manifest XML của định dạng `.appinstaller`, tự động tải gói `.msix`/`.appx` và kích hoạt `Add-AppxPackage` mượt mà.
  - Cập nhật **Minecraft X (XMCL)** sang gói `xmcl.appinstaller` chính thức siêu nhẹ, loại bỏ hoàn toàn tình trạng tải nhầm file nén nặng nề.
- **Tối ưu hóa Launcher:**
  - Cập nhật launcher một dòng lệnh duy nhất: `irm bit.ly/phongvntv | iex`.
  - Khắc phục triệt để lỗi khóa tệp (`IOException: Access Denied`) khi launcher cập nhật phiên bản mới đè lên thư mục đang chạy.

---

### [v1.1.5] — 14/10/2026
- **3 Chế độ thực thi phần mềm độc lập:**
  - Bổ sung thanh chọn chế độ: *⚡ Cài tự động*, *🖥️ Hiện Setup*, *💾 Chỉ tải file*.
  - Cho phép chọn thư mục lưu trữ file cài đặt tải về cho người dùng thích lưu trữ bộ cài offline.
- **Cửa sổ PowerShell Live Log thời gian thực:**
  - Tích hợp cửa sổ dòng lệnh PowerShell stream toàn bộ tiến trình cài đặt với mã màu chuyên nghiệp tương tự WinUtil.
- **Tự động làm sạch tệp tin rác:**
  - Tự động quét và xóa sạch các tệp cấu hình `.yaml` sinh ra trong quá trình winget tải manifest.
- **Nâng cấp toàn diện 13 phần mềm cốt lõi:**
  - **Zalo PC**: Chuyển sang nguồn tải trực tiếp chuẩn `https://zalo.me/download/zalo-pc`, lưu đúng định dạng `.exe`.
  - **Steam**: Thêm cơ chế dự phòng tải trực tiếp từ CDN khi DNS bị chặn.
  - **Genshin Impact**: Nâng cấp sang trình khởi chạy hợp nhất HoYoPlay PC chính thức.
  - **Office 365, Python, AnyDesk, UltraViewer, EVKey, RevoltG**: Cập nhật link tải và cơ chế cài đặt mới nhất.
  - **ChatGPT, Codex, GoodNotes**: Tích hợp sâu vào Microsoft Store qua Winget msstore source.

---

### [v1.1.0 – v1.1.4] — 13/10/2026
- **Ra mắt Menu TWEAKS & ACTIVATION:**
  - Bổ sung công cụ tinh chỉnh hệ thống Chris Titus WinUtil.
  - Tích hợp công cụ kích hoạt bản quyền mã nguồn mở MAS (Windows/Office) và IAS (IDM).
  - Tích hợp script tự động kích hoạt WinRAR bằng registry key bản quyền.
- **Cải tiến giao diện & Điều hướng:**
  - Nâng cấp giao diện Dark Theme tràn viền cao cấp.
  - Thêm thanh tìm kiếm ứng dụng tức thì và bộ lọc theo nhóm danh mục.
  - Bổ sung các phím thao tác nhanh *Select All* và *Clear*.
- **Tự động nâng quyền Administrator:**
  - Ứng dụng tự động phát hiện và kích hoạt chế độ quản trị viên khi cần thực thi các tác vụ hệ thống.

---

### [v1.0.0] — 12/10/2026 *(Bản phát hành đầu tiên)*
- Ra mắt công cụ **WinMaster** với mục tiêu đơn giản hóa quy trình cài đặt máy tính Windows mới.
- Hơn 60 ứng dụng được phân loại rõ ràng trong tệp cấu hình `applications.json`.
- Cơ chế cài đặt tuần tự thông minh (Sequential Installer) ngăn chặn xung đột tiến trình cài đặt.
- Tích hợp bộ kiểm tra tính toàn vẹn (Verification Engine) xác nhận ứng dụng đã được cài đặt thành công sau mỗi bước.

---

## 🗺️ Lộ trình phát triển (Roadmap)

| Phiên bản | Trọng tâm & Tính năng | Trạng thái |
|:---:|---|:---:|
| **v1.0.0** | Giao diện cài đặt một chạm, kiến trúc tuần tự, tích hợp winget & nguồn cố định | ✅ Hoàn thành |
| **v1.1.0** | Menu TWEAKS, tối ưu hóa hệ điều hành, kích hoạt Windows / Office / IDM / WinRAR | ✅ Hoàn thành |
| **v1.1.5** | 3 chế độ cài đặt, PowerShell Live Log, dọn dẹp YAML, chuẩn hóa 13 ứng dụng | ✅ Hoàn thành |
| **v1.1.6** | Chuẩn hóa vị trí cài đặt mặc định, nút xóa log Tweak, hỗ trợ gói .appinstaller | ✅ Hoàn thành |
| **v1.1.7** | Thêm Win11Debloat vào menu Tweaks | ✅ Hoàn thành |
| **v1.2.0** | Menu WinPE: Bộ công cụ tạo USB cứu hộ, sao lưu và khôi phục Windows | 🚀 Kế hoạch |
---

## 🛡️ Cam kết an toàn & Bảo mật

- **Minh bạch mã nguồn:** WinMaster mã nguồn mở 100%, bạn có thể tự kiểm tra mọi dòng code và script trong repository.
- **Nguồn tải chính thống:** Chỉ tải phần mềm từ các trang chủ chính thức của nhà phát triển, CDN xác thực hoặc kho lưu trữ winget của Microsoft.
- **Không Telemetry:** Hoàn toàn không thu thập thông tin người dùng, không gửi dữ liệu ra bên ngoài.
- **Quyền hạn tối thiểu:** Chỉ yêu cầu quyền Administrator đối với các tác vụ can thiệp vào hệ thống cần thiết.

---

## 📄 Bản quyền (License)

Dự án được phân phối dưới giấy phép **MIT License**. Chi tiết xem tại tệp [LICENSE](LICENSE).

<div align="center">
  <br/>
  <p>Được phát triển với tất cả tâm huyết dành cho cộng đồng người dùng Windows.</p>
  <p><strong>WinMaster</strong> — TICK → RUN → DONE</p>
</div>
