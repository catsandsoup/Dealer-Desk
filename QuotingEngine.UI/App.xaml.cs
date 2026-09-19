using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using QuotingEngine.UI;
using QuotingEngine.Core.Models;
using QuotingEngine.Infrastructure.Configuration;
using QuotingEngine.Infrastructure.Data;
using QuotingEngine.Core.Api;
using QuotingEngine.Infrastructure.Hardware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace QuotingEngine_UI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Window? CurrentWindow { get; private set; }
    public static IHost? Host { get; private set; }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                string baseFolder;
                try
                {
                    baseFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
                }
                catch
                {
                    baseFolder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "DealerDesk");
                }
                
                if (!System.IO.Directory.Exists(baseFolder))
                {
                    System.IO.Directory.CreateDirectory(baseFolder);
                }

                ConfigurationManager.Initialize(baseFolder);
                
                // Core / Infra
                var config = ConfigurationManager.Load();
                services.AddSingleton(config);
                
                // Initialize DbContext (we'll keep it simple for now as a singleton for the app lifecycle, 
                // but usually this is Scoped/Transient if we have multi-window concurrency)
                var dbPath = System.IO.Path.Combine(baseFolder, "quoting_engine.db");
                services.AddSingleton<AppDbContext>(sp => new AppDbContext(dbPath));
                
                // Services
                services.AddSingleton<SpotPriceClient>(sp => new SpotPriceClient(config.MetalPriceApiKey, config.BaseCurrency, config.MetalPriceCacheHours));
                services.AddSingleton<SerialPortReader>(sp => new SerialPortReader("COM1", 9600));

                // ViewModels
                services.AddTransient<QuotingEngine.UI.ViewModels.MainWindowViewModel>();
                
                // Windows (Transient so they can be recreated)
                services.AddTransient<ShellWindow>();
                services.AddTransient<SetupWizardWindow>();
            })
            .Build();

        // Global crash safety net — catch ALL unhandled exceptions so the app
        // never silently dies again. Log them for diagnostics.
        this.UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Mark as handled to prevent the default WER crash dialog
        e.Handled = true;
        System.Diagnostics.Debug.WriteLine($"[UNHANDLED EXCEPTION] {e.Exception}");
        
        // Also write to a crash log file for post-mortem analysis
        try
        {
            var logDir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "DealerDesk");
            if (!System.IO.Directory.Exists(logDir))
                System.IO.Directory.CreateDirectory(logDir);
            
            var logPath = System.IO.Path.Combine(logDir, "crash_log.txt");
            var entry = $"[{System.DateTime.UtcNow:O}] {e.Exception}\n\n";
            System.IO.File.AppendAllText(logPath, entry);
        }
        catch
        {
            // If even logging fails, there's nothing more we can do
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            Host?.Start();
            
            // Database initialization synchronously on startup to prevent SQLite thread locks.
            // EnsureCreated creates the schema and runs HasData seeds if the DB doesn't exist.
            var dbContext = Host?.Services.GetService(typeof(AppDbContext)) as AppDbContext;
            dbContext?.Database.EnsureCreated();

            var config = Host?.Services.GetService(typeof(DealerConfig)) as DealerConfig;
            
            if (config == null || !config.IsConfigured)
            {
                CurrentWindow = Host?.Services.GetService(typeof(SetupWizardWindow)) as Window;
            }
            else
            {
                CurrentWindow = Host?.Services.GetService(typeof(ShellWindow)) as Window;
            }
            
            CurrentWindow?.Activate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App.OnLaunched] Fatal: {ex}");
            CurrentWindow = new SetupWizardWindow();
            CurrentWindow.Activate();
        }
    }
}
