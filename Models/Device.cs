namespace Fusilone.Models;

public class Device
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string TechSpecs { get; set; } = string.Empty; // JSON format
    
    public DateTime LastMaintenanceDate { get; set; } = DateTime.Now;
    public int MaintenancePeriodMonths { get; set; } = 6;
    public DateTime NextMaintenanceDate { get; set; }
    public decimal Cost { get; set; } // Satın Alım Fiyatı
    public string Status { get; set; } = "Aktif"; // Aktif, Pasif, Hurda, Arızalı, Kayıp
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    public DateTime ManufactureDate { get; set; } = DateTime.Now; // Üretim Tarihi
    public DateTime CreatedDate { get; set; } = DateTime.Now; // Sisteme Eklenme Tarihi
    public int WarrantyPeriodMonths { get; set; } = 24; // Garanti Süresi (Ay)
    public string ImageUrl { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty; // Cihaz Adı / Takma Ad
    public string OwnerName { get; set; } = string.Empty;  // Sahibi / Zimmetli Kişi
    public int? OwnerCustomerId { get; set; }  // FK to Customers
    public string Notes { get; set; } = string.Empty; // Teknisyen notları / ekstralar

    public string FullName => $"{Brand} {Model}".Trim();
}
