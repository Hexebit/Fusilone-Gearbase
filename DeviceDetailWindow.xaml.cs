using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Fusilone.Data;
using Fusilone.Models;
using Fusilone.Helpers;

namespace Fusilone;

// Data Binding için yardımcı sınıf
public class SpecItem : INotifyPropertyChanged
{
    public string Key { get; set; } = "";

    private string _displayKey = "";
    public string DisplayKey
    {
        get => string.IsNullOrWhiteSpace(_displayKey) ? Key : _displayKey;
        set { _displayKey = value; OnPropertyChanged(nameof(DisplayKey)); }
    }
    
    private string _value = "";
    public string Value 
    { 
        get => _value; 
        set { _value = value; OnPropertyChanged(nameof(Value)); } 
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class DeviceDetailWindow : Window, INotifyPropertyChanged
{
    private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
    private readonly Device _device;
    private List<DevicePart> _deviceParts = new List<DevicePart>();
    private Dictionary<string, string> _originalSpecs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, List<string>> SpecFieldsByType = new(StringComparer.OrdinalIgnoreCase)
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
    
    // UI Binding Property
    private bool _isReadOnlyMode = true;
    public bool IsReadOnlyMode
    {
        get => _isReadOnlyMode;
        set { _isReadOnlyMode = value; OnPropertyChanged(nameof(IsReadOnlyMode)); }
    }

    public ObservableCollection<SpecItem> TechSpecsCollection { get; set; } = new ObservableCollection<SpecItem>();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public DeviceDetailWindow(Device device)
    {
        InitializeComponent();
        _device = device;
        DataContext = this; // Binding Context
        NewRecordDate.SelectedDate = DateTime.Now;
        
        LoadDeviceDetails();
        LoadTechSpecs(); 
        LoadMaintenanceHistory();
        LoadPhotos();
        LoadParts();
        LoadPartMovements();

        if (PartStatusCombo != null && PartStatusCombo.SelectedIndex < 0)
            PartStatusCombo.SelectedIndex = 0;
        if (PartMovementActionCombo != null && PartMovementActionCombo.SelectedIndex < 0)
            PartMovementActionCombo.SelectedIndex = 0;
    }

    private void LoadDeviceDetails()
    {
        IdHeader.Text = _device.Id;
        BrandBox.Text = _device.Brand;
        ModelBox.Text = _device.Model;
        TypeText.Text = _device.Type;
        SerialNumberBox.Text = _device.SerialNumber;
        DeviceNameBox.Text = _device.DeviceName;
        OwnerNameBox.Text = _device.OwnerName;
        NotesBox.Text = _device.Notes;
        PeriodBox.Text = _device.MaintenancePeriodMonths.ToString();
        LastMaintText.Text = _device.LastMaintenanceDate.ToString("dd.MM.yyyy");
        StatusChip.Content = SpecLocalization.GetStatusDisplayLabel(_device.Status);
        
        // Tarih bilgileri
        CreatedDateText.Text = $"Sisteme eklenme: {_device.CreatedDate.ToString("dd.MM.yyyy HH:mm")}";
        ManufactureDateText.Text = _device.ManufactureDate.ToString("dd.MM.yyyy");
        PurchaseDateText.Text = _device.PurchaseDate.ToString("dd.MM.yyyy");
        UpdateWarrantyStatus();

        // Set initial status in combo box
        foreach (ComboBoxItem item in StatusComboBox.Items)
        {
            if ((item.Tag?.ToString() ?? item.Content.ToString()) == _device.Status)
            {
                StatusComboBox.SelectedItem = item;
                break;
            }
        }

        // Set initial type in combo box
        foreach (ComboBoxItem item in TypeCombo.Items)
        {
            if (item.Content.ToString() == _device.Type)
            {
                TypeCombo.SelectedItem = item;
                break;
            }
        }

        if (!string.IsNullOrEmpty(_device.ImageUrl))
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(_device.ImageUrl, UriKind.Absolute);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                DeviceImage.Source = bitmap;
            }
            catch { }
        }
    }

    private void UpdateWarrantyStatus()
    {
        DateTime baseDate = GetWarrantyBaseDate();
        int warrantyMonths = _device.WarrantyPeriodMonths > 0 ? _device.WarrantyPeriodMonths : 24;
        DateTime warrantyEndDate = baseDate.AddMonths(warrantyMonths);
        bool isWarrantyActive = DateTime.Now.Date <= warrantyEndDate.Date;

        WarrantyStatusChip.Content = TryFindResource(isWarrantyActive ? "Warranty_Active" : "Warranty_Expired")?.ToString()
            ?? (isWarrantyActive ? "Devam Ediyor" : "Bitti");

        var activeBrush = TryFindResource("SuccessColor") as Brush ?? Brushes.ForestGreen;
        var expiredBrush = TryFindResource("ErrorColor") as Brush ?? Brushes.IndianRed;
        var foregroundBrush = TryFindResource("MaterialDesignPaper") as Brush ?? Brushes.White;

        WarrantyStatusChip.Background = isWarrantyActive ? activeBrush : expiredBrush;
        WarrantyStatusChip.Foreground = foregroundBrush;
        WarrantyEndDateText.Text = $"({TryFindResource("Detail_WarrantyUntil")?.ToString() ?? "Bitiş"}: {warrantyEndDate:dd.MM.yyyy})";
    }

    private DateTime GetWarrantyBaseDate()
    {
        if (_device.PurchaseDate > DateTime.MinValue.AddDays(1))
            return _device.PurchaseDate;

        if (_device.ManufactureDate > DateTime.MinValue.AddDays(1))
            return _device.ManufactureDate;

        if (_device.CreatedDate > DateTime.MinValue.AddDays(1))
            return _device.CreatedDate;

        return DateTime.Now;
    }

    private void LoadTechSpecs()
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var specs = JsonSerializer.Deserialize<Dictionary<string, string>>(_device.TechSpecs, options);
            // Ensure we have entries for common fields for this device type so empty fields can be edited later
            specs ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            EnsureCommonFieldsForType(_device.Type, specs);

            _originalSpecs = new Dictionary<string, string>(specs, StringComparer.OrdinalIgnoreCase);

            TechSpecsCollection.Clear();
            foreach (var kvp in specs)
            {
                TechSpecsCollection.Add(new SpecItem { Key = kvp.Key, DisplayKey = SpecLocalization.GetDisplayLabel(kvp.Key), Value = kvp.Value });
            }
            TechSpecsList.ItemsSource = TechSpecsCollection;
        }
        catch { }
    }

    private void EnsureCommonFieldsForType(string typeCode, Dictionary<string,string> specs)
    {
        if (!string.IsNullOrEmpty(typeCode) && SpecFieldsByType.TryGetValue(typeCode, out var fields))
        {
            foreach (var f in fields)
            {
                if (!specs.ContainsKey(f)) specs[f] = string.Empty;
            }
        }
        else
        {
            // fallback common keys
            foreach (var f in new[] { "CPU","RAM","Storage","GPU" })
            {
                if (!specs.ContainsKey(f)) specs[f] = string.Empty;
            }
        }
    }

    private void EditMode_Click(object sender, RoutedEventArgs e)
    {
        // Toggle Edit Mode
        IsReadOnlyMode = false; // Trigger UI update via Binding
        
        BrandBox.IsReadOnly = false;
        ModelBox.IsReadOnly = false;
        BrandBox.BorderThickness = new Thickness(0,0,0,1);
        ModelBox.BorderThickness = new Thickness(0,0,0,1);
        DeviceNameBox.BorderThickness = new Thickness(0,0,0,1);
        OwnerNameBox.BorderThickness = new Thickness(0,0,0,1);
        SerialNumberBox.BorderThickness = new Thickness(0,0,0,1);
        PeriodBox.BorderThickness = new Thickness(0,0,0,1);
        
        EditButton.Visibility = Visibility.Collapsed;
        SaveEditButton.Visibility = Visibility.Visible;
    }

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        _device.Brand = BrandBox.Text;
        _device.Model = ModelBox.Text;
        _device.DeviceName = DeviceNameBox.Text;
        _device.OwnerName = OwnerNameBox.Text;
        _device.SerialNumber = SerialNumberBox.Text;
        _device.Notes = NotesBox.Text;
        
        if (int.TryParse(PeriodBox.Text, out int period))
        {
            _device.MaintenancePeriodMonths = period;
        }
        
        if (TypeCombo.SelectedItem is ComboBoxItem typeItem)
        {
            _device.Type = typeItem.Content.ToString() ?? _device.Type;
            TypeText.Text = _device.Type;
        }

        // Update Status
        if (StatusComboBox.SelectedItem is ComboBoxItem selectedStatus)
        {
            _device.Status = selectedStatus.Tag?.ToString() ?? selectedStatus.Content.ToString() ?? "Aktif";
            StatusChip.Content = SpecLocalization.GetStatusDisplayLabel(_device.Status);
        }

        // Collect updated specs
        var newSpecs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in TechSpecsCollection)
        {
            if (!string.IsNullOrEmpty(item.Key))
                newSpecs[item.Key] = item.Value;
        }
        _device.TechSpecs = JsonSerializer.Serialize(newSpecs);

        var specChanges = GetSpecChanges(_originalSpecs, newSpecs);

        try
        {
            _dbHelper.UpdateDevice(_device);

            if (specChanges.Count > 0)
            {
                var record = new MaintenanceRecord
                {
                    DeviceId = _device.Id,
                    Date = DateTime.Now,
                    Description = "Teknik detay güncellendi",
                    Cost = 0,
                    Notes = string.Join("; ", specChanges)
                };
                _dbHelper.AddMaintenanceRecord(record);
                LoadMaintenanceHistory();
            }
            
            // Revert to ReadOnly
            IsReadOnlyMode = true;
            BrandBox.IsReadOnly = true;
            ModelBox.IsReadOnly = true;
            
            // UI Cleanup for new boxes
            BrandBox.BorderThickness = new Thickness(0);
            ModelBox.BorderThickness = new Thickness(0);
            DeviceNameBox.BorderThickness = new Thickness(0);
            OwnerNameBox.BorderThickness = new Thickness(0);
            SerialNumberBox.BorderThickness = new Thickness(0);
            PeriodBox.BorderThickness = new Thickness(0);
            
            EditButton.Visibility = Visibility.Visible;
            SaveEditButton.Visibility = Visibility.Collapsed;
            
            _originalSpecs = new Dictionary<string, string>(newSpecs, StringComparer.OrdinalIgnoreCase);
            MessageBox.Show("Tüm değişiklikler kaydedildi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Güncelleme hatası: {ex.Message}");
        }
    }

    private static List<string> GetSpecChanges(Dictionary<string, string> before, Dictionary<string, string> after)
    {
        var changes = new List<string>();

        foreach (var kvp in after)
        {
            var key = kvp.Key;
            var newVal = kvp.Value ?? "";
            before.TryGetValue(key, out var oldVal);
            oldVal ??= "";

            if (!string.Equals(oldVal, newVal, StringComparison.OrdinalIgnoreCase))
            {
                var label = SpecLocalization.GetDisplayLabel(key);
                if (string.IsNullOrWhiteSpace(oldVal) && !string.IsNullOrWhiteSpace(newVal))
                {
                    changes.Add($"{label}: {newVal}");
                }
                else if (!string.IsNullOrWhiteSpace(oldVal) && string.IsNullOrWhiteSpace(newVal))
                {
                    changes.Add($"{label}: {oldVal} → (silindi)");
                }
                else
                {
                    changes.Add($"{label}: {oldVal} → {newVal}");
                }
            }
        }

        return changes;
    }

    private void LoadMaintenanceHistory()
    {
        try
        {
            var records = _dbHelper.GetRecordsByDeviceId(_device.Id);
            HistoryDataGrid.ItemsSource = records;

            decimal purchasePrice = _device.Cost;
            decimal totalMaintCost = records.Sum(r => r.Cost);
            decimal totalCost = purchasePrice + totalMaintCost;

            if (PurchasePriceText != null) PurchasePriceText.Text = purchasePrice.ToString("C");
            if (TotalMaintCostText != null) TotalMaintCostText.Text = totalMaintCost.ToString("C");
            if (TotalCostText != null) TotalCostText.Text = totalCost.ToString("C");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Geçmiş yüklenirken hata: {ex.Message}");
        }
    }

    private void LoadPhotos()
    {
        try
        {
            var photos = _dbHelper.GetPhotosByDeviceId(_device.Id);
            string projectRoot = AppDomain.CurrentDomain.BaseDirectory;
            
            foreach (var p in photos)
            {
                string fullPath = Path.Combine(projectRoot, p.FilePath);
                if (File.Exists(fullPath)) p.FullPath = fullPath;
            }
            
            PhotoGallery.ItemsSource = photos.Where(x => !string.IsNullOrEmpty(x.FullPath)).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fotoğraflar yüklenirken hata: {ex.Message}");
        }
    }

    private void AddRecord_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewRecordDescription.Text))
        {
            MessageBox.Show("Lütfen işlem/arıza açıklaması girin.");
            return;
        }

        if (!decimal.TryParse(NewRecordCost.Text, out decimal cost)) cost = 0;

        var record = new MaintenanceRecord
        {
            DeviceId = _device.Id,
            Date = NewRecordDate.SelectedDate ?? DateTime.Now,
            Description = NewRecordDescription.Text,
            Cost = cost,
            Notes = ""
        };

        try
        {
            _dbHelper.AddMaintenanceRecord(record);
            
            // Sync Device object and Database
            _device.LastMaintenanceDate = record.Date;
            _dbHelper.UpdateDevice(_device);
            
            // Refresh UI
            LastMaintText.Text = record.Date.ToString("dd.MM.yyyy");
            
            NewRecordDescription.Clear();
            NewRecordCost.Text = "0";
            LoadMaintenanceHistory(); 
            MessageBox.Show("Bakım kaydı eklendi ve cihazın son bakım tarihi güncellendi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kayıt hatası: {ex.Message}");
        }
    }

    private void LoadParts()
    {
        try
        {
            _deviceParts = _dbHelper.GetDevicePartsByDeviceId(_device.Id);
            PartsDataGrid.ItemsSource = _deviceParts;
            PartMovementPartCombo.ItemsSource = _deviceParts;

            if (_deviceParts.Count > 0 && PartMovementPartCombo.SelectedIndex < 0)
            {
                PartMovementPartCombo.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Parçalar yüklenirken hata: {ex.Message}");
        }
    }

    private void LoadPartMovements()
    {
        try
        {
            PartMovementsDataGrid.ItemsSource = _dbHelper.GetPartMovementsByDeviceId(_device.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Parça hareketleri yüklenirken hata: {ex.Message}");
        }
    }

    private void AddPart_Click(object sender, RoutedEventArgs e)
    {
        string name = PartNameTextBox.Text?.Trim() ?? "";
        string number = PartNumberTextBox.Text?.Trim() ?? "";
        string notes = PartNotesTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Lütfen parça adı girin.");
            return;
        }

        if (!int.TryParse(PartQtyTextBox.Text, out int qty)) qty = 1;

        string status = "";
        if (PartStatusCombo.SelectedItem is ComboBoxItem statusItem)
        {
            status = statusItem.Tag?.ToString() ?? statusItem.Content?.ToString() ?? "";
        }

        try
        {
            var part = _dbHelper.UpsertDevicePart(_device.Id, name, number, qty, status, notes);
            string addedLabel = Application.Current?.Resources["Parts_Action_Added"] as string ?? "Added";
            _dbHelper.AddPartMovement(new PartMovement
            {
                DeviceId = _device.Id,
                PartName = part.PartName,
                Action = addedLabel,
                Quantity = Math.Max(0, qty),
                Cost = 0,
                Notes = notes,
                Date = DateTime.Now
            });

            PartNameTextBox.Clear();
            PartNumberTextBox.Clear();
            PartQtyTextBox.Text = "1";
            PartNotesTextBox.Clear();

            LoadParts();
            LoadPartMovements();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Parça ekleme hatası: {ex.Message}");
        }
    }

    private void AddPartMovement_Click(object sender, RoutedEventArgs e)
    {
        if (PartMovementPartCombo.SelectedItem is not DevicePart part)
        {
            MessageBox.Show("Lütfen bir parça seçin.");
            return;
        }

        if (!int.TryParse(PartMovementQtyTextBox.Text, out int qty)) qty = 1;
        if (!decimal.TryParse(PartMovementCostTextBox.Text, out decimal cost)) cost = 0;

        string actionCode = "";
        string actionDisplay = "";
        if (PartMovementActionCombo.SelectedItem is ComboBoxItem actionItem)
        {
            actionCode = actionItem.Tag?.ToString() ?? "";
            actionDisplay = actionItem.Content?.ToString() ?? actionCode;
        }

        if (string.IsNullOrWhiteSpace(actionCode))
        {
            MessageBox.Show("Lütfen hareket türü seçin.");
            return;
        }

        string notes = PartMovementNotesTextBox.Text?.Trim() ?? "";

        try
        {
            int delta = actionCode == "Added" ? qty : actionCode == "Removed" ? -qty : 0;
            if (delta != 0)
            {
                int newQty = Math.Max(0, part.Quantity + delta);
                _dbHelper.UpdateDevicePartQuantity(part.Id, newQty);
            }

            _dbHelper.AddPartMovement(new PartMovement
            {
                DeviceId = _device.Id,
                PartName = part.PartName,
                Action = actionDisplay,
                Quantity = Math.Max(0, qty),
                Cost = cost,
                Notes = notes,
                Date = DateTime.Now
            });

            PartMovementQtyTextBox.Text = "1";
            PartMovementCostTextBox.Text = "0";
            PartMovementNotesTextBox.Clear();

            LoadParts();
            LoadPartMovements();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hareket ekleme hatası: {ex.Message}");
        }
    }

    private void AddPhoto_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Resimler|*.jpg;*.jpeg;*.png;*.bmp";
        
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                string source = openFileDialog.FileName;
                string ext = Path.GetExtension(source);
                string newName = $"{Guid.NewGuid()}{ext}";
                
                string targetDir = Path.Combine(PathHelper.GetImagesPath(), _device.Id);

                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string destPath = Path.Combine(targetDir, newName);
                File.Copy(source, destPath);

                string relPath = Path.Combine("Images", _device.Id, newName);
                
                var photo = new DevicePhoto
                {
                    DeviceId = _device.Id,
                    FilePath = relPath,
                    Description = Path.GetFileNameWithoutExtension(source)
                };

                _dbHelper.AddDevicePhoto(photo);
                LoadPhotos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yükleme hatası: {ex.Message}");
            }
        }
    }

    private void CreateLabel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // If in edit mode, sync data first (but don't necessarily save to DB unless user clicks Save)
            if (!IsReadOnlyMode)
            {
                _device.Brand = BrandBox.Text;
                _device.Model = ModelBox.Text;
                
                var currentSpecs = new Dictionary<string, string>();
                foreach (var item in TechSpecsCollection)
                {
                    if (!string.IsNullOrEmpty(item.Key))
                        currentSpecs[item.Key] = item.Value;
                }
                _device.TechSpecs = JsonSerializer.Serialize(currentSpecs);
            }

            string path = "";
            
            // Get last maintenance note
            var records = _dbHelper.GetRecordsByDeviceId(_device.Id);
            string note = "Henüz bakım yapılmadı";
            if (records != null && records.Count > 0)
            {
                var lastRecord = records.OrderByDescending(r => r.Date).FirstOrDefault();
                if (lastRecord != null)
                {
                    if (!string.IsNullOrWhiteSpace(lastRecord.Notes))
                    {
                        note = lastRecord.Notes;
                    }
                    else if (!string.IsNullOrWhiteSpace(lastRecord.Description))
                    {
                        note = lastRecord.Description;
                    }
                }
            }

            path = LabelManager.GenerateLabel(_device, note);
            
            // To prevent potential UI lock or stale file handle issues, 
            // the user is notified and the file is overwritten on disk.
            MessageBox.Show($"Etiket güncellendi ve oluşturuldu!\nKonum: {path}", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Etiket hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }
}