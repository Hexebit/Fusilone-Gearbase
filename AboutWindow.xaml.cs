using System.Diagnostics;
using System.Windows;

namespace Fusilone;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://github.com/Hexebit");

    private void OpenWebsite_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://www.fusilone.com");

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { /* Tarayıcı açılamazsa sessizce geç */ }
    }
}
