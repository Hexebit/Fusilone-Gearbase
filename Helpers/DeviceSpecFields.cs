namespace Fusilone.Helpers;

/// <summary>
/// Cihaz türüne göre teknik özellik alan tanımları.
/// Hem "Yeni Cihaz Ekle" formu (MainWindow) hem de cihaz detayı (DeviceDetailWindow)
/// aynı listeyi kullanır; alan eklerken/çıkarırken yalnızca burası değişir.
/// </summary>
public static class DeviceSpecFields
{
    /// <summary>Karmaşık türlerde (PC/LP/ET) temel panelde gösterilen alanlar; kalanı "gelişmiş" bölümüne gider.</summary>
    public static readonly List<string> BasicFields = new()
    {
        "CPU Markası", "CPU Modeli", "Anakart Markası", "Anakart Modeli", "RAM Türü", "İşletim Sistemi", "RAM Boyutu (Toplam)", "Hafıza (Toplam)"
    };

    public static readonly Dictionary<string, List<string>> ByType = new(StringComparer.OrdinalIgnoreCase)
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
}
