using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.Json;
using System.Text;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using ClosedXML.Excel;
using Fusilone.Data;
using Fusilone.Models;
using Fusilone.Helpers;
using MaterialDesignThemes.Wpf;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Fusilone;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
    private List<Device> _allDevices = new List<Device>(); // Cache for filtering
    private List<CustomerRow> _customerRows = new List<CustomerRow>();

    private class CustomerRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public int TotalDevices { get; set; }
        public decimal TotalCost { get; set; }
        public string TotalCostDisplay => TotalCost.ToString("C0");
    }

    // Alan Tanımları
    private readonly Dictionary<string, List<string>> _fieldDefinitions = new()
    {
        { "PC", new List<string> {
            "CPU Markası", "CPU Modeli", "Anakart Markası", "Anakart Modeli", "RAM Türü", "İşletim Sistemi", "RAM Boyutu (Toplam)", "Hafıza (Toplam)",
            "Dahili Ekran Kartı Markası", "Dahili Ekran Kartı Modeli", "Dahili Ekran Kartı Bellek Miktarı",
            "Harici Ekran Kartı Markası", "Harici Ekran Kartı Modeli", "Harici Ekran Kartı Bellek Miktarı",
            "RAM Markası", "RAM Modeli (Slot 1)", "RAM Modeli (Slot 2)", "RAM Modeli (Slot 3)", "RAM Modeli (Slot 4)",
            "Ana Depolama Cihazı Markası", "Ana Depolama Cihazı Modeli", "Ana Depolama Cihazı Türü",
            "İkincil Depolama Cihazı Markası", "İkincil Depolama Cihazı Modeli", "İkincil Depolama Cihazı Türü",
            "PSU Markası", "PSU Modeli", "PSU Gücü", "Soğutma Tipi",
            "DVD-CD Sürücü Markası", "DVD-CD Sürücü Modeli", "BIOS versiyonu", "Wifi Desteği", "Bluetooth Desteği"
        }},
        { "AO", new List<string> {
            "CPU Markası", "CPU Modeli", "Anakart Markası", "Anakart Modeli", "RAM Türü", "İşletim Sistemi", "RAM Boyutu (Toplam)", "Hafıza (Toplam)",
            "Dahili Ekran Kartı Markası", "Dahili Ekran Kartı Modeli", "Dahili Ekran Kartı Bellek Miktarı",
            "Harici Ekran Kartı Markası", "Harici Ekran Kartı Modeli", "Harici Ekran Kartı Bellek Miktarı",
            "RAM Markası", "RAM Modeli (Slot 1)", "RAM Modeli (Slot 2)", "RAM Modeli (Slot 3)", "RAM Modeli (Slot 4)",
            "Ana Depolama Cihazı Markası", "Ana Depolama Cihazı Modeli", "Ana Depolama Cihazı Türü",
            "İkincil Depolama Cihazı Markası", "İkincil Depolama Cihazı Modeli", "İkincil Depolama Cihazı Türü",
            "Güç Adaptörü Markası", "Güç Adaptörü Modeli", "Adaptör Gücü",
            "Soğutma Tipi", "DVD-CD Sürücü Markası", "DVD-CD Sürücü Modeli", "BIOS versiyonu",
            "Wifi Desteği", "Bluetooth Desteği", "Monitör Panel Tipi", "Monitör Ekran Çözünürlüğü", "Kamera desteği"
        }},
        { "LP", new List<string> {
            "CPU Markası", "CPU Modeli", "Anakart Markası", "Anakart Modeli", "RAM Türü", "İşletim Sistemi", "RAM Boyutu (Toplam)", "Hafıza (Toplam)",
            "Dahili Ekran Kartı Markası", "Dahili Ekran Kartı Modeli", "Dahili Ekran Kartı Bellek Miktarı",
            "Harici Ekran Kartı Markası", "Harici Ekran Kartı Modeli", "Harici Ekran Kartı Bellek Miktarı",
            "RAM Markası", "RAM Modeli (Slot 1)", "RAM Modeli (Slot 2)", "RAM Modeli (Slot 3)", "RAM Modeli (Slot 4)",
            "Ana Depolama Cihazı Markası", "Ana Depolama Cihazı Modeli", "Ana Depolama Cihazı Türü",
            "İkincil Depolama Cihazı Markası", "İkincil Depolama Cihazı Modeli", "İkincil Depolama Cihazı Türü",
            "Güç Adaptörü Markası", "Güç Adaptörü Modeli", "Adaptör Gücü",
            "Soğutma Tipi", "DVD-CD Sürücü Markası", "DVD-CD Sürücü Modeli", "BIOS versiyonu",
            "Wifi Desteği", "Bluetooth Desteği", "Monitör Panel Tipi", "Monitör Ekran Çözünürlüğü", "Kamera desteği"
        }},
        { "ET", new List<string> {
            "CPU Markası", "CPU Modeli", "Anakart Markası", "Anakart Modeli", "RAM Türü", "İşletim Sistemi", "RAM Boyutu (Toplam)",
            "Dahili Ekran Kartı Markası", "Dahili Ekran Kartı Modeli", "Dahili Ekran Kartı Bellek Miktarı",
            "RAM Markası", "RAM Modeli (Slot 1)", "RAM Modeli (Slot 2)",
            "Ana Depolama Cihazı Markası", "Ana Depolama Cihazı Modeli", "Ana Depolama Cihazı Türü",
            "İkincil Depolama Cihazı Markası", "İkincil Depolama Cihazı Modeli", "İkincil Depolama Cihazı Türü",
            "Soğutma Tipi", "DVD-CD Sürücü Markası", "DVD-CD Sürücü Modeli", "BIOS versiyonu",
            "Wifi Desteği", "Bluetooth Desteği", "Monitör Panel Tipi", "Monitör Ekran Çözünürlüğü", "Kamera desteği"
        }},
        { "MN", new List<string> { "Ekran Çözünürlüğü", "Ekran Boyutu", "Panel Tipi", "Desteklenen Görüntü Portları", "Adaptör Türü", "Hoparlör özelliğinin olup olmadığı", "Yenileme Hızı" } },
        { "PH", new List<string> { "CPU Markası", "Ram Miktarı", "Depolama Miktarı", "İşletim Sistemi", "Batarya Kapasitesi", "IMEI", "Şarj Port Türü" } },
        { "TB", new List<string> { "CPU Markası", "Ram Miktarı", "Depolama Miktarı", "İşletim Sistemi", "Batarya Kapasitesi", "IMEI", "Şarj Port Türü" } },
        { "PR", new List<string> { "Yazıcı Türü (Toner ya da Kartuş)", "Renk Desteği", "Fonksiyonlar (Sadece yazıcı ya da Yazıcı ve Tarayıcı gibi)", "Sarf Malzemesi Modeli", "Bağlantı Türü" } },
        { "RT", new List<string> { "Router Türü (Modem, Router ya da Bridge gibi)", "WanType", "Admin kullanıcı adı", "wifi Bandı" } },
        { "PJ", new List<string> { "Çözünürlük", "Yenileme Hızı", "Desteklenen Görüntü Portları", "Adaptör Türü" } },
        { "GC", new List<string> { "Konsol Nesli", "Depolama Miktarı", "Optik Sürücü Durumu", "Kontrolcü Sayısı", "Çevrimiçi Servis", "Wifi Desteği", "Bluetooth Desteği" } }
    };

    private readonly List<string> _basicFields = new() {
        "CPU Markası", "CPU Modeli", "Anakart Markası", "Anakart Modeli", "RAM Türü", "İşletim Sistemi", "RAM Boyutu (Toplam)", "Hafıza (Toplam)"
    };

    private string _currentViewKey = "View_Dashboard";

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            _dbHelper.InitializeDatabase();
            LoadDeviceList(); 
            PurchaseDatePicker.SelectedDate = DateTime.Now; // Set Default
            ManufactureDatePicker.SelectedDate = DateTime.Now;
            WarrantyPeriodTextBox.Text = "24";
            
            // Sync ToggleButton with current theme
            var paletteHelper = new PaletteHelper();
            ThemeToggle.IsChecked = paletteHelper.GetTheme().GetBaseTheme() == BaseTheme.Dark;
            SetViewTitle("View_Dashboard");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Başlatma Hatası: {ex.Message}");
        }
    }

    // --- WINDOW CONTROL ---
    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        catch
        {
            // Ignore DragMove failures when mouse state is not valid
        }
    }

    private void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP (*.zip)|*.zip",
            FileName = $"Fusilone-Backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            string dbPath = PathHelper.GetDatabasePath();
            string imagesPath = PathHelper.GetImagesPath();
            string labelsPath = PathHelper.GetLabelsPath();

            if (File.Exists(dialog.FileName))
            {
                File.Delete(dialog.FileName);
            }

            using var zip = ZipFile.Open(dialog.FileName, ZipArchiveMode.Create);

            if (File.Exists(dbPath))
            {
                zip.CreateEntryFromFile(dbPath, "Data/fusilone.db", CompressionLevel.Optimal);
            }

            AddDirectoryToZip(zip, imagesPath, "Images");
            AddDirectoryToZip(zip, labelsPath, "Etiketler");

            var metaEntry = zip.CreateEntry("backup-info.txt", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(metaEntry.Open(), Encoding.UTF8))
            {
                writer.WriteLine("Fusilone Backup");
                writer.WriteLine($"Created: {DateTime.Now:O}");
                writer.WriteLine($"Contains: Data/fusilone.db, Images, Etiketler");
            }

            MessageBox.Show("Yedekleme tamamlandı.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yedekleme hatası: {ex.Message}");
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"Fusilone-Devices-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var devices = _dbHelper.GetAllDevices();
            using var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8);

            writer.WriteLine("Id,Type,Brand,Model,SerialNumber,DeviceName,OwnerName,OwnerCustomerId,Status,Cost,PurchaseDate,LastMaintenanceDate,NextMaintenanceDate,MaintenancePeriodMonths,ImageUrl,TechSpecs");

            foreach (var d in devices)
            {
                writer.WriteLine(string.Join(",", new[]
                {
                    CsvEscape(d.Id),
                    CsvEscape(d.Type),
                    CsvEscape(d.Brand),
                    CsvEscape(d.Model),
                    CsvEscape(d.SerialNumber),
                    CsvEscape(d.DeviceName),
                    CsvEscape(d.OwnerName),
                    CsvEscape(d.OwnerCustomerId?.ToString() ?? ""),
                    CsvEscape(d.Status),
                    CsvEscape(d.Cost.ToString("0.##")),
                    CsvEscape(d.PurchaseDate.ToString("o")),
                    CsvEscape(d.LastMaintenanceDate.ToString("o")),
                    CsvEscape(d.NextMaintenanceDate.ToString("o")),
                    CsvEscape(d.MaintenancePeriodMonths.ToString()),
                    CsvEscape(d.ImageUrl),
                    CsvEscape(d.TechSpecs)
                }));
            }

            MessageBox.Show("CSV dışa aktarımı tamamlandı.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CSV dışa aktarım hatası: {ex.Message}");
        }
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Fusilone-Devices-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var devices = _dbHelper.GetAllDevices();
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Devices");

            var headers = new[]
            {
                "Id", "Type", "Brand", "Model", "SerialNumber", "DeviceName", "OwnerName", "OwnerCustomerId", "Status",
                "Cost", "PurchaseDate", "LastMaintenanceDate", "NextMaintenanceDate", "MaintenancePeriodMonths", "ImageUrl", "TechSpecs"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var d in devices)
            {
                ws.Cell(row, 1).Value = d.Id;
                ws.Cell(row, 2).Value = d.Type;
                ws.Cell(row, 3).Value = d.Brand;
                ws.Cell(row, 4).Value = d.Model;
                ws.Cell(row, 5).Value = d.SerialNumber;
                ws.Cell(row, 6).Value = d.DeviceName;
                ws.Cell(row, 7).Value = d.OwnerName;
                ws.Cell(row, 8).Value = d.OwnerCustomerId?.ToString() ?? "";
                ws.Cell(row, 9).Value = d.Status;
                ws.Cell(row, 10).Value = (double)d.Cost;
                ws.Cell(row, 11).Value = d.PurchaseDate;
                ws.Cell(row, 12).Value = d.LastMaintenanceDate;
                ws.Cell(row, 13).Value = d.NextMaintenanceDate;
                ws.Cell(row, 14).Value = d.MaintenancePeriodMonths;
                ws.Cell(row, 15).Value = d.ImageUrl;
                ws.Cell(row, 16).Value = d.TechSpecs;
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(dialog.FileName);

            MessageBox.Show("Excel dışa aktarımı tamamlandı.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Excel dışa aktarım hatası: {ex.Message}");
        }
    }

    private static void AddDirectoryToZip(ZipArchive zip, string sourceDirectory, string entryRoot)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var entryPath = Path.Combine(entryRoot, relativePath).Replace('\\', '/');
            zip.CreateEntryFromFile(file, entryPath, CompressionLevel.Optimal);
        }
    }

    private static string CsvEscape(string value)
    {
        if (value == null)
        {
            return "";
        }

        bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (mustQuote)
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

    private void LangTR_Click(object sender, RoutedEventArgs e)
    {
        SwitchLanguage("Turkish.xaml");
    }

    private void LangEN_Click(object sender, RoutedEventArgs e)
    {
        SwitchLanguage("English.xaml");
    }

    private void SwitchLanguage(string fileName)
    {
        try
        {
            var app = Application.Current;
            var newDictUri = new Uri($"/Fusilone;component/Languages/{fileName}", UriKind.Relative);
            var newDict = new ResourceDictionary { Source = newDictUri };

            var existing = app.Resources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.IndexOf("/Languages/", StringComparison.OrdinalIgnoreCase) >= 0);
            if (existing != null)
            {
                app.Resources.MergedDictionaries.Remove(existing);
            }

            app.Resources.MergedDictionaries.Add(newDict);
            SetViewTitle(_currentViewKey);
            RefreshSpecHints();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dil dosyası yüklenemedi: {ex.Message}", "Hata");
        }
    }

    private void RefreshSpecHints()
    {
        if (DynamicFieldsPanel == null || AdvancedFieldsPanel == null) return;

        foreach (var panel in new[] { DynamicFieldsPanel, AdvancedFieldsPanel })
        {
            foreach (var child in panel.Children)
            {
                if (child is TextBox tb && tb.Tag is string key)
                {
                    HintAssist.SetHint(tb, SpecLocalization.GetDisplayLabel(key));
                }
            }
        }
    }

    private void SetViewTitle(string resourceKey)
    {
        _currentViewKey = resourceKey;
        if (ViewTitle == null) return;

        if (Application.Current?.Resources[resourceKey] is string title)
        {
            ViewTitle.Text = title;
        }
    }

    // --- NAVIGATION LOGIC ---
    private void ShowList_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceListTransitioner == null || AddDeviceTransitioner == null || SearchTransitioner == null || MaintenanceTransitioner == null || CustomersTransitioner == null) return;
        
        SetViewTitle("View_Dashboard");
        HideAllTransitions();
        DeviceListTransitioner.Visibility = Visibility.Visible;
        LoadDeviceList(); 
    }

    private void ShowSearch_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceListTransitioner == null || AddDeviceTransitioner == null || SearchTransitioner == null || MaintenanceTransitioner == null || CustomersTransitioner == null) return;
        
        SetViewTitle("View_DeviceSearch");
        HideAllTransitions();
        SearchTransitioner.Visibility = Visibility.Visible;
        LoadDeviceList();
    }

    private void ShowAdd_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceListTransitioner == null || AddDeviceTransitioner == null || SearchTransitioner == null || MaintenanceTransitioner == null || CustomersTransitioner == null) return;
        
        SetViewTitle("View_AddDevice");
        HideAllTransitions();
        AddDeviceTransitioner.Visibility = Visibility.Visible;
    }

    private void MaintenanceButton_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceListTransitioner == null || AddDeviceTransitioner == null || SearchTransitioner == null || MaintenanceTransitioner == null || CustomersTransitioner == null) return;
        
        SetViewTitle("View_Maintenance");
        HideAllTransitions();
        MaintenanceTransitioner.Visibility = Visibility.Visible;
        LoadMaintenanceList();
    }

    private void ShowCustomers_Click(object sender, RoutedEventArgs e)
    {
        if (DeviceListTransitioner == null || AddDeviceTransitioner == null || SearchTransitioner == null || MaintenanceTransitioner == null || CustomersTransitioner == null) return;

        SetViewTitle("View_Customers");
        HideAllTransitions();
        CustomersTransitioner.Visibility = Visibility.Visible;
        LoadCustomersList();
    }

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWin = new AboutWindow();
        aboutWin.Owner = this;
        aboutWin.ShowDialog();
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();

        if (theme.GetBaseTheme() == BaseTheme.Dark)
        {
            theme.SetBaseTheme(BaseTheme.Light);
        }
        else
        {
            theme.SetBaseTheme(BaseTheme.Dark);
        }

        paletteHelper.SetTheme(theme);
    }

    private void HideAllTransitions()
    {
        DeviceListTransitioner.Visibility = Visibility.Collapsed;
        AddDeviceTransitioner.Visibility = Visibility.Collapsed;
        SearchTransitioner.Visibility = Visibility.Collapsed;
        MaintenanceTransitioner.Visibility = Visibility.Collapsed;
        CustomersTransitioner.Visibility = Visibility.Collapsed;
    }

    private void LoadMaintenanceList()
    {
        try
        {
            var all = _dbHelper.GetAllDevices();
            var sorted = all.OrderBy(d => d.NextMaintenanceDate).ToList();
            MaintenanceDataGrid.ItemsSource = sorted;
        }
        catch { }
    }

    private void LoadCustomersList(string? filter = null)
    {
        try
        {
            var customers = _dbHelper.GetAllCustomers();
            _customerRows = customers.Select(c =>
            {
                var stats = _dbHelper.GetCustomerStats(c.Id);
                return new CustomerRow
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    TotalDevices = stats.TotalDevices,
                    TotalCost = stats.TotalCost
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                _customerRows = _customerRows
                    .Where(r => r.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || r.Email.Contains(filter, StringComparison.OrdinalIgnoreCase) || r.Phone.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            CustomersDataGrid.ItemsSource = _customerRows;

            if (_customerRows.Count > 0)
            {
                CustomersDataGrid.SelectedIndex = 0;
            }
            else
            {
                CustomerDevicesDataGrid.ItemsSource = new List<Device>();
            }
        }
        catch { }
    }

    private void CustomersSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string filter = CustomersSearchBox.Text?.Trim() ?? "";
        LoadCustomersList(filter);
    }

    private void CustomersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomersDataGrid.SelectedItem is CustomerRow row)
        {
            var devices = _dbHelper.GetDevicesByCustomerId(row.Id);
            CustomerDevicesDataGrid.ItemsSource = devices;
        }
    }
    // ------------------------

    // --- DASHBOARD / LIST LOGIC ---
    private void KanbanToggle_Click(object sender, RoutedEventArgs e)
    {
        if (KanbanToggle == null || KanbanBoard == null || DevicesDataGrid == null) return;

        if (KanbanToggle.IsChecked == true)
        {
            DevicesDataGrid.Visibility = Visibility.Collapsed;
            KanbanBoard.Visibility = Visibility.Visible;
            KanbanBoard.SetDevices(_allDevices);
        }
        else
        {
            DevicesDataGrid.Visibility = Visibility.Visible;
            KanbanBoard.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadDeviceList()
    {
        try
        {
            _allDevices = _dbHelper.GetAllDevices(); // Cache data
            
            // --- STATISTICS ---
            if (StatTotalCount != null) 
                StatTotalCount.Text = _allDevices.Count.ToString();
            
            if (StatActiveCount != null)
                StatActiveCount.Text = _allDevices.Count(d => d.Status == "Aktif").ToString();
            
            if (StatMaintDueCount != null)
                StatMaintDueCount.Text = _allDevices.Count(d => d.NextMaintenanceDate <= DateTime.Now.AddDays(7)).ToString(); 

            if (StatTotalValue != null)
                StatTotalValue.Text = _allDevices.Sum(d => d.Cost).ToString("C0");

            // --- CHART ---
            if (DeviceTypeChart != null)
            {
                var typeGroups = _allDevices.GroupBy(d => d.Type)
                                            .Select(g => new { Type = g.Key, Count = g.Count() })
                                            .ToList();

                // Deterministic colors per device type so colors don't shuffle each refresh
                SKColor GetColor(string t)
                {
                    var map = new Dictionary<string, SKColor>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "PC", new SKColor(0x4E,0xAE,0xF0) },
                        { "LP", new SKColor(0xFF,0xA7,0x26) },
                        { "AO", new SKColor(0x9C,0x27,0xB0) },
                        { "PH", new SKColor(0x03,0xA9,0xF4) },
                        { "ET", new SKColor(0xF4,0x43,0x36) },
                        { "MN", new SKColor(0x8B,0xC3,0x4A) }
                    };
                    if (map.TryGetValue(t, out var c)) return c;
                    var h = Math.Abs(t.GetHashCode());
                    byte r = (byte)((h >> 16) & 0xFF);
                    byte g = (byte)((h >> 8) & 0xFF);
                    byte b = (byte)(h & 0xFF);
                    return new SKColor(r, g, b);
                }

                var series = typeGroups.Select(group => new PieSeries<int>
                {
                    Name = group.Type,
                    Values = new[] { group.Count },
                    Fill = new SolidColorPaint(GetColor(group.Type))
                }).ToArray();

                DeviceTypeChart.Series = series;
            }

            // --- KANBAN REFRESH ---
            if (KanbanBoard != null && KanbanBoard.Visibility == Visibility.Visible)
            {
                KanbanBoard.SetDevices(_allDevices);
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Liste yüklenirken hata: " + ex.Message);
        }
    }

    private void EditDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string deviceId)
        {
            var device = _allDevices.FirstOrDefault(d => d.Id == deviceId);
            if (device != null)
            {
                var detailWindow = new DeviceDetailWindow(device);
                detailWindow.Owner = this;
                detailWindow.ShowDialog();
                LoadDeviceList();
            }
        }
    }

    private void DeleteDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string deviceId)
        {
            var result = MessageBox.Show($"Cihazı ({deviceId}) silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbHelper.DeleteDevice(deviceId);
                    MessageBox.Show("Cihaz silindi.");
                    LoadDeviceList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Silme hatası: {ex.Message}");
                }
            }
        }
    }

    private void SpecFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (SearchBox == null || FilterTypeCombo == null || FilterStatusCombo == null || DevicesDataGrid == null || SearchResultsDataGrid == null)
            return;

        var filtered = _allDevices.AsEnumerable();

        // 1. Text Filter (Search)
        string searchText = SearchBox.Text?.ToLower().Trim() ?? "";
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(d => 
                (d.Id != null && d.Id.ToLower().Contains(searchText)) || 
                (d.Brand != null && d.Brand.ToLower().Contains(searchText)) || 
                (d.Model != null && d.Model.ToLower().Contains(searchText)) ||
                (d.SerialNumber != null && d.SerialNumber.ToLower().Contains(searchText))
            );
        }

        // 2. Type Filter
        if (FilterTypeCombo.SelectedItem is ComboBoxItem typeItem)
        {
            string selectedType = typeItem.Tag?.ToString() ?? typeItem.Content.ToString()!;
            if (!string.Equals(selectedType, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(d => d.Type == selectedType);
            }
        }

        // 3. Status Filter
        if (FilterStatusCombo.SelectedItem is ComboBoxItem statusItem)
        {
            string selectedStatus = statusItem.Tag?.ToString() ?? statusItem.Content.ToString()!;
            if (!string.Equals(selectedStatus, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(d => d.Status == selectedStatus);
            }
        }

        // 4. Detailed Spec Filter
        if (SpecKeyBox != null && SpecValueBox != null)
        {
            string specKey = SpecKeyBox.Text?.ToLower().Trim() ?? "";
            string specValue = SpecValueBox.Text?.ToLower().Trim() ?? "";

            if (!string.IsNullOrEmpty(specValue))
            {
                filtered = filtered.Where(d => 
                {
                    if (string.IsNullOrEmpty(d.TechSpecs)) return false;
                    return d.TechSpecs.ToLower().Contains(specValue); 
                    // Note: A simpler contains check. For precise Key-Value check, JSON parsing is needed per item (slower).
                });
            }
        }

        var resultList = filtered.ToList();
        DevicesDataGrid.ItemsSource = resultList;
        SearchResultsDataGrid.ItemsSource = resultList;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void DevicesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid dg && dg.SelectedItem is Device selectedDevice)
        {
            var detailWindow = new DeviceDetailWindow(selectedDevice);
            detailWindow.Owner = this;
            // match main window size to provide consistent UX
            if (this.ActualWidth > 0 && this.ActualHeight > 0)
            {
                detailWindow.Width = this.ActualWidth;
                detailWindow.Height = this.ActualHeight;
                detailWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            detailWindow.ShowDialog();
            LoadDeviceList(); // Refresh after closing details (in case of updates)
            LoadMaintenanceList(); // Also refresh maintenance if we are in that view
        }
    }
    // -----------------------------

    // --- ADD DEVICE FORM LOGIC ---
    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DynamicFieldsPanel == null || AdvancedFieldsPanel == null || AdvancedSpecsExpander == null) return;

        DynamicFieldsPanel.Children.Clear();
        AdvancedFieldsPanel.Children.Clear();
        AdvancedSpecsExpander.Visibility = Visibility.Collapsed;

        if (TypeComboBox.SelectedItem is not ComboBoxItem selectedItem) return;

        string typeContent = selectedItem.Content.ToString() ?? "";
        string typeCode = typeContent.Split(' ')[0]; // PC, LP, etc.

        if (_fieldDefinitions.TryGetValue(typeCode, out var fields))
        {
            bool isComplexType = (typeCode == "PC" || typeCode == "LP" || typeCode == "ET");
            if (isComplexType) AdvancedSpecsExpander.Visibility = Visibility.Visible;

            foreach (var field in fields)
            {
                var textBox = new TextBox
                {
                    Tag = field,
                    Margin = new Thickness(0, 0, 0, 15),
                };
                
                textBox.SetResourceReference(Control.ForegroundProperty, "MaterialDesignBody");

                HintAssist.SetHint(textBox, SpecLocalization.GetDisplayLabel(field));
                textBox.Style = (Style)FindResource("MaterialDesignFloatingHintTextBox");

                if (isComplexType && !_basicFields.Contains(field))
                {
                    AdvancedFieldsPanel.Children.Add(textBox);
                }
                else
                {
                    DynamicFieldsPanel.Children.Add(textBox);
                }
            }
        }
    }

    private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private Device? CreateDeviceFromForm()
    {
        if (TypeComboBox.SelectedItem is not ComboBoxItem selectedTypeItem) return null;

        string typeFull = selectedTypeItem.Content.ToString() ?? "";
        string typeCode = typeFull.Split(' ')[0]; 
        string brand = BrandTextBox.Text;
        string model = ModelTextBox.Text;
        string serialNumber = SerialNumberTextBox.Text;

        if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(serialNumber))
        {
             return null;
        }

        DateTime lastMaint = LastMaintenanceDatePicker.SelectedDate ?? DateTime.Now;
        if (!int.TryParse(PeriodTextBox.Text, out int period)) period = 6; 
        DateTime nextMaint = lastMaint.AddMonths(period);

        if (!decimal.TryParse(CostTextBox.Text, out decimal cost)) cost = 0;

        // New Fields
        string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                ?? (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                ?? "Aktif";
        DateTime purchaseDate = PurchaseDatePicker.SelectedDate ?? DateTime.Now;
        DateTime manufactureDate = ManufactureDatePicker.SelectedDate ?? DateTime.Now;
        DateTime createdDate = DateTime.Now; // Sisteme eklenme tarihi
        if (!int.TryParse(WarrantyPeriodTextBox.Text, out int warrantyPeriodMonths) || warrantyPeriodMonths <= 0)
            warrantyPeriodMonths = 24;

        var techSpecs = new Dictionary<string, string>();
        
        // Collect from both panels
        var panels = new List<StackPanel> { DynamicFieldsPanel, AdvancedFieldsPanel };
        foreach (var panel in panels)
        {
            foreach (var child in panel.Children)
            {
                if (child is TextBox tb && tb.Tag != null)
                {
                    string fieldName = tb.Tag.ToString() ?? "";
                    if (!string.IsNullOrEmpty(tb.Text))
                        techSpecs[fieldName] = tb.Text;
                }
            }
        }

        string jsonSpecs = JsonSerializer.Serialize(techSpecs);
        
        int nextNumber = _dbHelper.GetNextSequenceNumber(typeCode);
        string id = $"FSL-{typeCode}-{nextNumber:D3}"; 

        int? ownerCustomerId = null;
        string ownerName = OwnerComboBox?.Text?.Trim() ?? "";
        string ownerEmail = OwnerEmailTextBox?.Text?.Trim() ?? "";
        string ownerPhone = OwnerPhoneTextBox?.Text?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(ownerName))
        {
            var customer = _dbHelper.UpsertCustomer(ownerName, ownerEmail, ownerPhone);
            ownerCustomerId = customer.Id;
            ownerName = customer.Name;
        }

        return new Device
        {
            Id = id,
            Type = typeCode,
            Brand = brand,
            Model = model,
            SerialNumber = serialNumber,
            TechSpecs = jsonSpecs,
            LastMaintenanceDate = lastMaint,
            MaintenancePeriodMonths = period,
            NextMaintenanceDate = nextMaint,
            Cost = cost,
            Status = status,
            PurchaseDate = purchaseDate,
            ManufactureDate = manufactureDate,
            CreatedDate = createdDate,
            WarrantyPeriodMonths = warrantyPeriodMonths,
            ImageUrl = ImageUrlTextBox.Text,
            DeviceName = DeviceNameTextBox.Text,
            OwnerName = ownerName,
            OwnerCustomerId = ownerCustomerId
        };
    }

    private async void AutoImageSearch_Click(object sender, RoutedEventArgs e)
    {
        string query = $"{BrandTextBox.Text} {ModelTextBox.Text}".Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            MessageBox.Show("Lütfen önce Marka ve Model giriniz.");
            return;
        }

        try
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            string? imageUrl = await ImageScraper.GetDeviceImageUrl(query);
            
            if (!string.IsNullOrEmpty(imageUrl))
            {
                ImageUrlTextBox.Text = imageUrl;
                // TextChanged event will trigger preview update
            }
            else
            {
                MessageBox.Show("Görsel bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Arama hatası: {ex.Message}");
        }
        finally
        {
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
    }

    private void ImageUrlTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PreviewImage == null) return;

        string url = ImageUrlTextBox.Text;
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) && 
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uriResult;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                PreviewImage.Source = bitmap;
                PreviewImage.Visibility = Visibility.Visible;
            }
            catch
            {
                PreviewImage.Source = null;
            }
        }
        else
        {
            PreviewImage.Source = null;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var device = CreateDeviceFromForm();
        if (device == null)
        {
            MessageBox.Show("Lütfen tüm alanları doldurun ve bir tür seçin.");
            return;
        }

        try
        {
            _dbHelper.AddDevice(device);
            // Generate label/QR on first save
            try
            {
                LabelManager.GenerateLabel(device);
            }
            catch
            {
                // Non-blocking: label generation failure shouldn't prevent save
            }
            MessageBox.Show($"Cihaz başarıyla kaydedildi!\nID: {device.Id}");
            
            // Clear fields
            BrandTextBox.Clear();
            ModelTextBox.Clear();
            SerialNumberTextBox.Clear();
            TypeComboBox.SelectedIndex = -1;
            DynamicFieldsPanel.Children.Clear();
            AdvancedFieldsPanel.Children.Clear();
            CostTextBox.Text = "0";
            PeriodTextBox.Text = "6";
            WarrantyPeriodTextBox.Text = "24";
            PurchaseDatePicker.SelectedDate = DateTime.Now;
            ManufactureDatePicker.SelectedDate = DateTime.Now;
            ImageUrlTextBox.Clear();
            DeviceNameTextBox.Clear();
            if (OwnerComboBox != null) OwnerComboBox.Text = "";
            OwnerEmailTextBox.Clear();
            OwnerPhoneTextBox.Clear();
            PreviewImage.Source = null;
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Hata oluştu: {ex.Message}");
        }
    }

    private void CreateLabelButton_Click(object sender, RoutedEventArgs e)
    {
        var device = CreateDeviceFromForm();
        if (device == null)
        {
            MessageBox.Show("Etiket oluşturmak için formu doldurmalısınız.");
            return;
        }

        try
        {
            string path = LabelManager.GenerateLabel(device);
            MessageBox.Show($"Etiket oluşturuldu!\nYol: {path}");
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Etiket oluşturulurken hata: {ex.Message}");
        }
    }

    private void OwnerComboBox_TextChanged(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (OwnerComboBox == null) return;

        string name = OwnerComboBox.Text?.Trim() ?? "";
        if (name.Length < 2)
        {
            return;
        }

        var matches = _dbHelper.SearchCustomersByName(name);
        OwnerComboBox.ItemsSource = matches.Select(c => c.Name).ToList();

        var exact = matches.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            OwnerEmailTextBox.Text = exact.Email;
            OwnerPhoneTextBox.Text = exact.Phone;
        }
    }

    private void ViewToggle_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder for view toggle logic if needed in code-behind
    }
}
