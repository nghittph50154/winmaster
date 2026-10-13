using System.Collections.ObjectModel;
using System.Diagnostics;

using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinMaster.Models;
using WinMaster.Services;

namespace WinMaster.ViewModels;

public partial class TweaksViewModel : ObservableObject
{
    private readonly LogService _logService;

    public ObservableCollection<TweakOptionViewModel> Options { get; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    [ObservableProperty]
    private TweakOptionViewModel? _selectedOption;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _currentStatusMessage = "Sẵn sàng";

    public bool CanRun => SelectedOption is not null && !IsRunning;

    public TweaksViewModel(LogService logService)
    {
        _logService = logService;
        _logService.EntryAdded += OnLogEntryAdded;

        InitializeOptions();
    }

    private void InitializeOptions()
    {
        var opt1 = new TweakOptionViewModel(
            "win_office",
            "Windows & Office Activation",
            "Chạy script kích hoạt bản quyền Windows và Office qua MAS (Microsoft Activation Scripts).",
            "powershell iex (irm https://get.activated.win)",
            "cmd",
            showCode: true);

        var opt2 = new TweakOptionViewModel(
            "winutil",
            "WinUtil (Chris Titus Tech)",
            "Mở công cụ WinUtil hỗ trợ Tweak, tối ưu hóa hệ thống Windows và cài đặt phần mềm.",
            "irm \"https://christitus.com/win\" | iex",
            "powershell",
            showCode: true);

        var opt3 = new TweakOptionViewModel(
            "idm_act",
            "IDM Activation (IAS)",
            "Kích hoạt và tự động cập nhật bản quyền Internet Download Manager (IDM).",
            "irm https://coporton.com/ias | iex",
            "powershell",
            showCode: true);

        var opt4 = new TweakOptionViewModel(
            "winrar_act",
            "WinRAR Activation",
            "Tự động tải và cài đặt bản quyền rarreg.key vào thư mục WinRAR trên máy.",
            "irm https://raw.githubusercontent.com/nghittph50154/winmaster/main/scripts/winrar.ps1 | iex",
            "powershell",
            showCode: false);

        Options.Add(opt1);
        Options.Add(opt2);
        Options.Add(opt3);
        Options.Add(opt4);

        // Default selection: option 1
        opt1.IsSelected = true;
        SelectedOption = opt1;
    }

    public void SelectOption(TweakOptionViewModel target)
    {
        foreach (var opt in Options)
        {
            opt.IsSelected = (opt == target);
        }
        SelectedOption = target;
        OnPropertyChanged(nameof(CanRun));
        RunCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (SelectedOption is null) return;

        IsRunning = true;
        CurrentStatusMessage = $"Đang khởi chạy {SelectedOption.Title}...";
        OnPropertyChanged(nameof(CanRun));
        RunCommand.NotifyCanExecuteChanged();

        _logService.Info($"─── Khởi chạy Tweak: {SelectedOption.Title} ───");
        _logService.Info($"Môi trường: {SelectedOption.ShellType.ToUpper()} (Admin Elevated)");

        try
        {
            await Task.Run(() =>
            {
                var isCmd = SelectedOption.ShellType.Equals("cmd", StringComparison.OrdinalIgnoreCase);
                string exePath;
                string arguments;

                if (isCmd)
                {
                    exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
                    if (!File.Exists(exePath)) exePath = "cmd.exe";
                    arguments = $"/k {SelectedOption.CommandText}";
                }
                else
                {
                    exePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
                    if (!File.Exists(exePath)) exePath = "powershell.exe";
                    arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{SelectedOption.CommandText}\"";
                }

                // Try Direct process execution first (bypasses ShellExecute token/elevation conflicts)
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetTempPath(),
                        UseShellExecute = false,
                        CreateNoWindow = false
                    };

                    using var proc = Process.Start(psi);
                    if (proc is not null)
                    {
                        _logService.Success($"Đã mở cửa sổ thực thi {SelectedOption.Title} thành công!");
                        return;
                    }
                }
                catch
                {
                    // Fallback to ShellExecute
                }

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetTempPath(),
                        UseShellExecute = true
                    };

                    using var proc = Process.Start(psi);
                    if (proc is not null)
                    {
                        _logService.Success($"Đã mở cửa sổ thực thi {SelectedOption.Title} thành công!");
                    }
                    else
                    {
                        _logService.Error("Không thể khởi chạy tiến trình.");
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error($"Lỗi khi chạy script: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logService.Error($"Lỗi khi chạy script: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
            CurrentStatusMessage = "Sẵn sàng";
            OnPropertyChanged(nameof(CanRun));
            RunCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnLogEntryAdded(object? sender, LogEntry entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(entry);
        });
    }
}
