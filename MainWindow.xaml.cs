using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.Json;
using Microsoft.Win32;
using Fusilone.Data;
using Fusilone.Models;
using Fusilone.Helpers;
using Serilog;
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

    private void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "ZIP (*.zip)|*.zip",
            FileName = $"Gearbase-Backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            DeviceExporter.CreateBackup(
                dialog.FileName,
                PathHelper.GetDatabasePath(),
                PathHelper.GetImagesPath(),
                PathHelper.GetLabelsPath());

            MessageBox.Show("Yedekleme tamamlandı.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Yedekleme hatası");
            MessageBox.Show($"Yedekleme hatası: {ex.Message}");
        }
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"Gearbase-Devices-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            DeviceExporter.ExportCsv(_dbHelper.GetAllDevices(), dialog.FileName);
            MessageBox.Show("CSV dışa aktarımı tamamlandı.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CSV dışa aktarım hatası");
            MessageBox.Show($"CSV dışa aktarım hatası: {ex.Message}");
        }
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Gearbase-Devices-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            DeviceExporter.ExportExcel(_dbHelper.GetAllDevices(), dialog.FileName);
            MessageBox.Show("Excel dışa aktarımı tamamlandı.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Excel dışa aktarım hatası");
            MessageBox.Show($"Excel dışa aktarım hatası: {ex.Message}");
        }
    }

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
    private void ShowList_Click(object sender, RoutedEventArgs e) =>
        SwitchView("View_Dashboard", DeviceListTransitioner, LoadDeviceList);

    private void ShowSearch_Click(object sender, RoutedEventArgs e) =>
        SwitchView("View_DeviceSearch", SearchTransitioner, LoadDeviceList);

    private void ShowAdd_Click(object sender, RoutedEventArgs e) =>
        SwitchView("View_AddDevice", AddDeviceTransitioner);

    private void MaintenanceButton_Click(object sender, RoutedEventArgs e) =>
        SwitchView("View_Maintenance", MaintenanceTransitioner, LoadMaintenanceList);

    private void ShowCustomers_Click(object sender, RoutedEventArgs e) =>
        SwitchView("View_Customers", CustomersTransitioner, () => LoadCustomersList());

    // Tüm görünüm geçişlerinin ortak iskeleti: başlığı ayarla, hepsini gizle,
    // hedefi göster ve varsa ilgili veriyi yükle.
    private void SwitchView(string titleKey, UIElement target, Action? onShown = null)
    {
        if (DeviceListTransitioner == null || AddDeviceTransitioner == null || SearchTransitioner == null
            || MaintenanceTransitioner == null || CustomersTransitioner == null) return;

        SetViewTitle(titleKey);
        HideAllTransitions();
        target.Visibility = Visibility.Visible;
        onShown?.Invoke();
    }

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
    {
        var aboutWin = new AboutWindow();
        aboutWin.Owner = this;
        aboutWin.ShowDialog();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        // Maximize durumunda pencere kenarları ekran dışına taşar (7px); telafi et
        MainRootBorder.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // İlk açılışta pencere ekranın çalışma alanına sığmıyorsa küçült ve ortala
        var workArea = SystemParameters.WorkArea;
        if (Width > workArea.Width) Width = workArea.Width;
        if (Height > workArea.Height) Height = workArea.Height;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top + (workArea.Height - Height) / 2;
    }

    private void MinButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void MaxButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void TitleBarClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
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

        // Swap Fusilone theme dictionary
        var app = (App)Application.Current;
        var dictionaries = app.Resources.MergedDictionaries;
        var fusiloneDict = dictionaries.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Fusilone.Dark") == true ||
            d.Source?.OriginalString.Contains("Fusilone.Light") == true);

        if (fusiloneDict != null)
        {
            int index = dictionaries.IndexOf(fusiloneDict);
            string newTheme = theme.GetBaseTheme() == BaseTheme.Dark ? "Dark" : "Light";
            dictionaries[index] = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Fusilone;component/Themes/Fusilone.{newTheme}.xaml")
            };
        }
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
        catch (Exception ex)
        {
            Log.Error(ex, "Bakım listesi yüklenirken hata");
        }
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
        catch (Exception ex)
        {
            Log.Error(ex, "Müşteri listesi yüklenirken hata");
        }
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
            // Kanban modu: liste ve boş durum panelleri gizlenir
            DevicesDataGrid.Visibility = Visibility.Collapsed;
            if (InventoryEmptyState != null) InventoryEmptyState.Visibility = Visibility.Collapsed;
            if (InventoryFilteredEmptyState != null) InventoryFilteredEmptyState.Visibility = Visibility.Collapsed;
            KanbanBoard.Visibility = Visibility.Visible;
            KanbanBoard.SetDevices(_allDevices);
        }
        else
        {
            // Liste modu: görünürlüğü boş duruma göre ApplyFilters belirler
            KanbanBoard.Visibility = Visibility.Collapsed;
            DevicesDataGrid.Visibility = Visibility.Visible;
            UpdateInventoryEmptyState((DevicesDataGrid.ItemsSource as System.Collections.ICollection)?.Count ?? 0);
        }
    }

    private void LoadDeviceList()
    {
        try
        {
            _allDevices = _dbHelper.GetAllDevices(); // Veriyi önbelleğe al

            UpdateStatistics();
            UpdateDistributionChart();

            if (KanbanBoard != null && KanbanBoard.Visibility == Visibility.Visible)
            {
                KanbanBoard.SetDevices(_allDevices);
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Cihaz listesi yüklenirken hata");
            MessageBox.Show("Liste yüklenirken hata: " + ex.Message);
        }
    }

    // Dashboard üstündeki 4 istatistik kartını günceller.
    private void UpdateStatistics()
    {
        if (StatTotalCount != null)
            StatTotalCount.Text = _allDevices.Count.ToString();

        if (StatActiveCount != null)
            StatActiveCount.Text = _allDevices.Count(d => d.Status == "Aktif").ToString();

        if (StatMaintDueCount != null)
        {
            int maintDue = _allDevices.Count(d => d.NextMaintenanceDate <= DateTime.Now.AddDays(7));
            StatMaintDueCount.Text = maintDue.ToString();

            // Bakım bekleyen cihaz varsa chip danger renklerine geçer
            if (MaintChip != null && MaintChipIcon != null)
            {
                string chipBg = maintDue > 0 ? "FslDangerFaint" : "FslSurface2";
                string chipFg = maintDue > 0 ? "FslDanger" : "FslTextSecondary";
                MaintChip.SetResourceReference(Border.BackgroundProperty, chipBg);
                MaintChipIcon.SetResourceReference(Control.ForegroundProperty, chipFg);
            }
        }

        if (StatTotalValue != null)
            StatTotalValue.Text = _allDevices.Sum(d => d.Cost).ToString("C0");
    }

    // Fusilone teal skalasında seri renkleri; alfabetik tür sırasına göre atanır.
    private static readonly SKColor[] ChartTealScale =
    {
        new(0x17, 0xA3, 0x98),
        new(0x1F, 0xBF, 0xB2),
        new(0x0F, 0x6E, 0x67),
        new(0x6B, 0xD9, 0xCF),
        new(0x12, 0x85, 0x7C)
    };

    // Cihaz dağılımı pastasını günceller. Tek kategori varsa grafik yerine boş durum gösterir.
    private void UpdateDistributionChart()
    {
        if (DeviceTypeChart == null) return;

        var typeGroups = _allDevices.GroupBy(d => d.Type)
                                    .Select(g => new { Type = g.Key, Count = g.Count() })
                                    .OrderBy(g => g.Type)
                                    .ToList();

        // Tek kategoriyle pasta grafik anlamsız: grafiği gizle, boş durum göster
        if (typeGroups.Count < 2)
        {
            DeviceTypeChart.Visibility = Visibility.Collapsed;
            if (ChartEmptyState != null)
            {
                ChartEmptyState.Visibility = Visibility.Visible;
                if (ChartEmptyDetail != null)
                    ChartEmptyDetail.Text = typeGroups.Count == 1
                        ? $"{typeGroups[0].Type}: {typeGroups[0].Count}"
                        : string.Empty;
            }
            return;
        }

        DeviceTypeChart.Visibility = Visibility.Visible;
        if (ChartEmptyState != null)
            ChartEmptyState.Visibility = Visibility.Collapsed;

        DeviceTypeChart.Series = typeGroups.Select((group, i) => new PieSeries<int>
        {
            Name = group.Type,
            Values = new[] { group.Count },
            Fill = new SolidColorPaint(ChartTealScale[i % ChartTealScale.Length])
        }).ToArray();

        // Legend yazıları aktif temanın ikincil metin rengini alır
        if (TryFindResource("FslTextSecondary") is SolidColorBrush legendBrush)
        {
            var lc = legendBrush.Color;
            DeviceTypeChart.LegendTextPaint = new SolidColorPaint(new SKColor(lc.R, lc.G, lc.B));
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

        UpdateInventoryEmptyState(resultList.Count);
    }

    // Envanter boş durumları: hiç cihaz yok / filtre eşleşmesi yok / normal liste
    private void UpdateInventoryEmptyState(int visibleCount)
    {
        if (InventoryEmptyState == null || InventoryFilteredEmptyState == null || DevicesDataGrid == null)
            return;

        bool listMode = KanbanToggle == null || KanbanToggle.IsChecked != true;

        if (_allDevices.Count == 0)
        {
            // Hiç cihaz yok: CTA'lı boş durum
            InventoryEmptyState.Visibility = Visibility.Visible;
            InventoryFilteredEmptyState.Visibility = Visibility.Collapsed;
            DevicesDataGrid.Visibility = Visibility.Collapsed;
        }
        else if (visibleCount == 0)
        {
            // Cihaz var ama filtre eşleşmiyor
            InventoryEmptyState.Visibility = Visibility.Collapsed;
            InventoryFilteredEmptyState.Visibility = Visibility.Visible;
            DevicesDataGrid.Visibility = Visibility.Collapsed;
        }
        else
        {
            InventoryEmptyState.Visibility = Visibility.Collapsed;
            InventoryFilteredEmptyState.Visibility = Visibility.Collapsed;
            DevicesDataGrid.Visibility = listMode ? Visibility.Visible : Visibility.Collapsed;
        }
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

        if (DeviceSpecFields.ByType.TryGetValue(typeCode, out var fields))
        {
            bool isComplexType = (typeCode == "PC" || typeCode == "LP" || typeCode == "ET");
            if (isComplexType) AdvancedSpecsExpander.Visibility = Visibility.Visible;

            foreach (var field in fields)
            {
                var textBox = new TextBox
                {
                    Tag = field,
                    Margin = new Thickness(0, 0, 0, 15),
                    Style = (Style)FindResource("FslTextBox")
                };

                HintAssist.SetHint(textBox, SpecLocalization.GetDisplayLabel(field));

                if (isComplexType && !DeviceSpecFields.BasicFields.Contains(field))
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

}