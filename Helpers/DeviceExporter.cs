using System.IO;
using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Fusilone.Models;

namespace Fusilone.Helpers;

/// <summary>
/// Cihaz verisinin dışa aktarımı (CSV, Excel) ve uygulama yedeği (ZIP) için
/// UI'dan bağımsız iş mantığı. MainWindow yalnızca dosya diyaloğunu açıp bunları çağırır.
/// </summary>
public static class DeviceExporter
{
    private static readonly string[] Headers =
    {
        "Id", "Type", "Brand", "Model", "SerialNumber", "DeviceName", "OwnerName", "OwnerCustomerId", "Status",
        "Cost", "PurchaseDate", "LastMaintenanceDate", "NextMaintenanceDate", "MaintenancePeriodMonths", "ImageUrl", "TechSpecs"
    };

    public static void ExportCsv(IEnumerable<Device> devices, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        writer.WriteLine(string.Join(",", Headers));

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
    }

    public static void ExportExcel(IEnumerable<Device> devices, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Devices");

        for (int i = 0; i < Headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = Headers[i];
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
        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// Veritabanı, görseller ve etiketleri tek bir ZIP dosyasında yedekler.
    /// </summary>
    public static void CreateBackup(string zipPath, string dbPath, string imagesPath, string labelsPath)
    {
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        if (File.Exists(dbPath))
        {
            zip.CreateEntryFromFile(dbPath, "Data/gearbase.db", CompressionLevel.Optimal);
        }

        AddDirectoryToZip(zip, imagesPath, "Images");
        AddDirectoryToZip(zip, labelsPath, "Etiketler");

        var metaEntry = zip.CreateEntry("backup-info.txt", CompressionLevel.Optimal);
        using var metaWriter = new StreamWriter(metaEntry.Open(), Encoding.UTF8);
        metaWriter.WriteLine("Gearbase Backup");
        metaWriter.WriteLine($"Created: {DateTime.Now:O}");
        metaWriter.WriteLine("Contains: Data/gearbase.db, Images, Etiketler");
    }

    private static void AddDirectoryToZip(ZipArchive zip, string sourceDirectory, string entryRoot)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var entryPath = Path.Combine(entryRoot, relativePath).Replace('\\', '/');
            zip.CreateEntryFromFile(file, entryPath, CompressionLevel.Optimal);
        }
    }

    private static string CsvEscape(string? value)
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
}
