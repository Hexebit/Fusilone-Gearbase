using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Serilog;
using Fusilone.Helpers;

namespace Fusilone;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        InitializeLogging();
        Log.Information("=== Fusilone App Başlatılıyor ===");

        LoadLanguageResources();
        
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            MessageBox.Show($"Kritik Hata (AppDomain): {ex.ExceptionObject}");
        };

        this.DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show($"Uygulama Hatası (Dispatcher): {ex.Exception.Message}");
            ex.Handled = true;
        };
    }

    private void LoadLanguageResources()
    {
        string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string fileName = lang.Equals("tr", StringComparison.OrdinalIgnoreCase) ? "Turkish.xaml" : "English.xaml";

        var dict = new ResourceDictionary
        {
            Source = new Uri($"/Fusilone;component/Languages/{fileName}", UriKind.Relative)
        };

        var existing = Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Languages/", StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            Resources.MergedDictionaries.Remove(existing);
        }

        Resources.MergedDictionaries.Add(dict);
    }

    private void InitializeLogging()
    {
        string appDataPath = Helpers.PathHelper.GetAppDataPath();
        string logsPath = Path.Combine(appDataPath, "Logs");
        if (!Directory.Exists(logsPath))
            Directory.CreateDirectory(logsPath);

        string logPath = Path.Combine(logsPath, "fusilone-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();
    }
}