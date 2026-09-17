using System.Windows;
using WinMaster.Infrastructure;
using WinMaster.Installers;
using WinMaster.Services;
using WinMaster.ViewModels;
using WinMaster.Views;

namespace WinMaster;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // --- Dependency wiring (simple manual DI — no container needed for v1.0) ---
        var psRunner = new PowerShellRunner();
        var logService = new LogService();
        var networkService = new NetworkService();
        var adminService = new AdminService();
        var verificationService = new VerificationService(psRunner);

        var wingetInstaller = new WingetInstaller(psRunner);
        var directInstaller = new DirectInstaller();
        var fixedInstaller = new FixedSourceInstaller();
        var psInstaller = new PowerShellInstaller(psRunner);
        var pendingInstaller = new PendingInstaller();

        var engine = new InstallerEngine(
            wingetInstaller,
            directInstaller,
            fixedInstaller,
            psInstaller,
            pendingInstaller,
            verificationService,
            logService,
            networkService,
            adminService);

        var registry = new AppRegistryService();
        var installVM = new InstallViewModel(registry, engine, logService);
        var tweaksVM = new TweaksViewModel(logService);

        try
        {
            installVM.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load application registry:\n\n{ex.Message}\n\nPlease ensure applications.json exists.",
                "WinMaster — Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        var mainVM = new MainViewModel(installVM, tweaksVM);
        var mainWindow = new MainWindow { DataContext = mainVM };
        mainWindow.Show();
    }
}
