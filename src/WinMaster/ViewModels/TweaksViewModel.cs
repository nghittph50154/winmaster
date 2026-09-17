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
            "cmd");

        var opt2 = new TweakOptionViewModel(
            "winutil",
            "WinUtil (Chris Titus Tech)",
            "Mở công cụ WinUtil hỗ trợ Tweak, tối ưu hóa hệ thống Windows và cài đặt phần mềm.",
            "irm \"https://christitus.com/win\" | iex",
            "powershell");

        var opt3 = new TweakOptionViewModel(
            "idm_act",
            "IDM Activation (IAS)",
            "Kích hoạt và tự động cập nhật bản quyền Internet Download Manager (IDM).",
            "irm https://coporton.com/ias | iex",
            "powershell");

        var opt4 = new TweakOptionViewModel(
            "winrar_act",
            "WinRAR Activation",
            "Tự động tải và cài đặt bản quyền rarreg.key vào thư mục WinRAR trên máy.",
            "irm https://raw.githubusercontent.com/nghittph50154/winmaster/main/winrar.ps1 | iex",
            "powershell");

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
                ProcessStartInfo psi;

                if (SelectedOption.ShellType.Equals("cmd", StringComparison.OrdinalIgnoreCase))
                {
                    // Execute CMD elevated safely via PowerShell Start-Process wrapper
                    var cmdArgs = $"/c {SelectedOption.CommandText}";
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-Process cmd.exe -ArgumentList '{cmdArgs.Replace("'", "''")}' -Verb RunAs\"",
                        UseShellExecute = true
                    };
                }
                else
                {
                    // Execute via PowerShell with Admin privileges directly via Start-Process
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -Command \"\"\"{SelectedOption.CommandText.Replace("\"", "`\"")}\"\"\"' -Verb RunAs\"",
                        UseShellExecute = true
                    };
                }

                using var proc = Process.Start(psi);
                if (proc is null)
                {
                    _logService.Error("Không thể khởi chạy tiến trình với quyền Admin.");
                }
                else
                {
                    _logService.Success($"Đã mở cửa sổ thực thi {SelectedOption.Title} thành công!");
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
