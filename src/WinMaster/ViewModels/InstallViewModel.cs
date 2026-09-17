using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinMaster.Infrastructure;
using WinMaster.Installers;
using WinMaster.Models;
using WinMaster.Services;

namespace WinMaster.ViewModels;

/// <summary>
/// ViewModel for the Install menu — the primary view in WinMaster v1.0.
/// Manages application list, search, selection, and installation orchestration.
/// </summary>
public partial class InstallViewModel : ObservableObject
{
    private readonly AppRegistryService _registry;
    private readonly InstallerEngine _engine;
    private readonly LogService _logService;

    // All app card VMs (flat list, then grouped for display)
    private readonly List<ApplicationCardViewModel> _allApps = new();

    public InstallViewModel(AppRegistryService registry, InstallerEngine engine, LogService logService)
    {
        _registry = registry;
        _engine = engine;
        _logService = logService;

        // Wire engine events to UI updates
        _engine.ProgressChanged += OnProgressChanged;
        _engine.AppCompleted += OnAppCompleted;
        _engine.SessionCompleted += OnSessionCompleted;

        // Wire log service to observable collection
        _logService.EntryAdded += OnLogEntryAdded;
    }

    // ─── Collections ────────────────────────────────────────────────────────

    /// <summary>Grouped categories for display.</summary>
    public ObservableCollection<CategoryViewModel> Categories { get; } = new();

    /// <summary>Log entries for the log panel.</summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    // ─── Search ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCount))]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplySearch(value);

    // ─── Selection ──────────────────────────────────────────────────────────

    public int SelectedCount => _allApps.Count(a => a.IsSelected);

    public string SelectedCountText => SelectedCount == 0
        ? "No applications selected"
        : $"Selected: {SelectedCount} application{(SelectedCount == 1 ? "" : "s")}";

    // ─── Run state ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRun))]
    private bool _isRunning;

    partial void OnIsRunningChanged(bool value) => RunCommand.NotifyCanExecuteChanged();

    [ObservableProperty]
    private string _currentStatusMessage = "Ready";

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _sessionSummaryText = string.Empty;

    [ObservableProperty]
    private bool _showSummary;

    public bool CanRun => !IsRunning && SelectedCount > 0;

    // ─── Initialization ─────────────────────────────────────────────────────

    /// <summary>Loads applications from registry and builds the UI list synchronously.</summary>
    public void Initialize()
    {
        _registry.Load();

        _allApps.Clear();
        Categories.Clear();

        var categories = _registry.GetCategories();

        foreach (var category in categories)
        {
            var apps = _registry.GetByCategory(category)
                .Select(e => new ApplicationCardViewModel(e))
                .ToList();

            // Wire selection changed notification
            foreach (var app in apps)
            {
                app.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ApplicationCardViewModel.IsSelected))
                    {
                        OnPropertyChanged(nameof(SelectedCount));
                        OnPropertyChanged(nameof(SelectedCountText));
                        OnPropertyChanged(nameof(CanRun));
                        RunCommand.NotifyCanExecuteChanged();
                    }
                };
                _allApps.Add(app);
            }

            Categories.Add(new CategoryViewModel(category, apps));
        }
    }

    /// <summary>Loads applications from registry and builds the UI list.</summary>
    public async Task InitializeAsync()
    {
        Initialize();
        await Task.CompletedTask;
    }

    // ─── Commands ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var app in _allApps.Where(a => a.IsVisible))
            app.IsSelected = true;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedCountText));
        OnPropertyChanged(nameof(CanRun));
        RunCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var app in _allApps)
            app.IsSelected = false;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedCountText));
        OnPropertyChanged(nameof(CanRun));
        RunCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private string _customInstallPath = FileSystemHelper.GetDownloadsFolder();

    [ObservableProperty]
    private bool _isInteractiveMode = true;

    [RelayCommand]
    private void BrowseCustomInstallPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Custom Installation / Download Directory",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            CustomInstallPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void ClearCustomInstallPath()
    {
        CustomInstallPath = FileSystemHelper.GetDownloadsFolder();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        var selected = _allApps
            .Where(a => a.IsSelected)
            .ToList();

        if (!selected.Any()) return;

        // Reset all statuses
        foreach (var app in _allApps)
            app.ResetStatus();

        LogEntries.Clear();
        ShowSummary = false;
        SessionSummaryText = string.Empty;
        IsRunning = true;
        OverallProgress = 0;
        CurrentStatusMessage = "Starting installation...";

        using var cts = new CancellationTokenSource();

        try
        {
            var entries = selected.Select(a => a.Entry).ToList();
            await _engine.RunAsync(entries, CustomInstallPath, IsInteractiveMode, cts.Token);
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void CancelRun()
    {
        // TODO: propagate cancellation token in v1.1
        CurrentStatusMessage = "Cancelling...";
    }

    // ─── Engine Event Handlers ───────────────────────────────────────────────

    private void OnProgressChanged(object? sender, InstallProgress progress)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentStatusMessage = progress.StatusMessage;
            OverallProgress = progress.ProgressPercent;

            // Update the specific card's status
            var card = _allApps.FirstOrDefault(a => a.Id == progress.CurrentAppId);
            if (card is not null)
            {
                card.InstallStatus = progress.CurrentStatus;
                card.StatusMessage = progress.StatusMessage;
            }
        });
    }

    private void OnAppCompleted(object? sender, InstallResult result)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var card = _allApps.FirstOrDefault(a => a.Id == result.ApplicationId);
            if (card is not null)
            {
                card.InstallStatus = result.Status;
                card.StatusMessage = result.IsSuccess ? "Installed" : result.ErrorMessage ?? "Failed";
            }
        });
    }

    private void OnSessionCompleted(object? sender, InstallSessionSummary summary)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsRunning = false;
            OverallProgress = 100;
            CurrentStatusMessage = "Installation complete.";

            var lines = new List<string>
            {
                $"Installation complete — {summary.TotalCount} application(s) processed",
                $"✓ Success: {summary.SuccessCount}",
            };

            if (summary.FailedCount > 0)
            {
                lines.Add($"✗ Failed: {summary.FailedCount}");
                foreach (var f in summary.FailedResults)
                    lines.Add($"  • {f.ApplicationName}: {f.ErrorMessage}");
            }

            if (summary.SkippedCount > 0)
                lines.Add($"⚠ Skipped: {summary.SkippedCount}");

            SessionSummaryText = string.Join("\n", lines);
            ShowSummary = true;
        });
    }

    private void OnLogEntryAdded(object? sender, LogEntry entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(entry);
        });
    }

    // ─── Search Logic ────────────────────────────────────────────────────────

    private void ApplySearch(string query)
    {
        var lower = query.ToLowerInvariant().Trim();

        foreach (var category in Categories)
        {
            int visibleCount = 0;
            foreach (var app in category.Apps)
            {
                bool match = string.IsNullOrEmpty(lower)
                    || app.DisplayName.ToLowerInvariant().Contains(lower)
                    || app.Category.ToLowerInvariant().Contains(lower);

                app.IsVisible = match;
                if (match) visibleCount++;
            }
            category.IsVisible = visibleCount > 0;
        }
    }
}

/// <summary>
/// Groups applications by category for display.
/// </summary>
public partial class CategoryViewModel : ObservableObject
{
    public string Name { get; }
    public ObservableCollection<ApplicationCardViewModel> Apps { get; }

    [ObservableProperty]
    private bool _isVisible = true;

    public CategoryViewModel(string name, IEnumerable<ApplicationCardViewModel> apps)
    {
        Name = name;
        Apps = new ObservableCollection<ApplicationCardViewModel>(apps);
    }
}
