using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using Fusilone.Models;
using QRCoder;

namespace Fusilone.Helpers;

public class LabelManager
{
    // High Resolution: 3x Scale (Original 350x200 -> 1050x600)
    private const int BaseWidth = 350;
    private const int BaseHeight = 200;
    private const double ScaleFactor = 3.0;
    
    private const int Width = (int)(BaseWidth * ScaleFactor);   // 1050
    private const int Height = (int)(BaseHeight * ScaleFactor); // 600

    public static string GenerateLabel(Device device, string maintenanceNote = "")
    {
        string fileName = $"Etiket_{device.Id}.png";
        string outputFolder = PathHelper.GetLabelsPath();
        string assetsFolder = PathHelper.GetAssetsPath();
        string logoPath = Path.Combine(assetsFolder, "logo.png");

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        
        string fullPath = Path.Combine(outputFolder, fileName);

        // --- 1. PREPARE RESOURCES & MEASURE TEXT ---
        
        double padding = 15;
        double gap = 20;
        double qrSize = 140 * ScaleFactor;

        // Fonts
        Typeface fontFaceBold = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        Typeface fontFaceRegular = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        // Scaled Font Sizes
        double fontSizeTitle = 12 * ScaleFactor;
        double fontSizeID = 14 * ScaleFactor;
        double fontSizeName = 11 * ScaleFactor;
        double fontSizeDetail = 9 * ScaleFactor;
        double fontSizeSmall = 7 * ScaleFactor;

        // Helper to create FormattedText
        FormattedText CreateText(string text, Typeface typeface, double size, Brush brush)
        {
            return new FormattedText(
                text ?? "",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                size,
                brush,
                VisualTreeHelper.GetDpi(new ContainerVisual()).PixelsPerDip);
        }

        var textTitle = CreateText("FUSILONE GEARBASE", fontFaceBold, fontSizeTitle, Brushes.Black);
        var textId = CreateText(device.Id, fontFaceBold, fontSizeID, Brushes.Black);
        
        var textName = string.IsNullOrEmpty(device.DeviceName) ? null : 
                       CreateText(device.DeviceName, fontFaceBold, fontSizeName, Brushes.Black);

        string brandModelStr = $"{device.Brand} {device.Model}".Trim();
        var textBrandModel = CreateText(brandModelStr, fontFaceRegular, fontSizeDetail, Brushes.Black);

        double maxTextWidth = Width - (padding * 2) - gap - qrSize;

        string summaryStr = GetTechSummary(device);
        var textSummary = CreateText(summaryStr, fontFaceRegular, fontSizeSmall, Brushes.DarkGray);
        textSummary.MaxTextWidth = maxTextWidth;
        textSummary.MaxTextHeight = fontSizeSmall * 3.5;
        textSummary.Trimming = TextTrimming.CharacterEllipsis;

        string lastMaint = device.LastMaintenanceDate.ToString("dd.MM.yyyy");
        string nextMaint = device.NextMaintenanceDate.ToString("dd.MM.yyyy");
        var textDates1 = CreateText($"Son Bakım: {lastMaint}", fontFaceRegular, fontSizeSmall, Brushes.Black);
        var textDates2 = CreateText($"Sıradaki: {nextMaint}", fontFaceBold, fontSizeSmall, Brushes.Black);
        
        var textOwner = string.IsNullOrEmpty(device.OwnerName) ? null :
                        CreateText($"Sahibi: {device.OwnerName}", fontFaceBold, fontSizeSmall, Brushes.DarkBlue);

        string noteDisplay = maintenanceNote ?? string.Empty;
        var textNote = string.IsNullOrWhiteSpace(noteDisplay) ? null :
                       CreateText($"Not: {noteDisplay}", fontFaceRegular, fontSizeSmall, Brushes.DimGray);
        if (textNote != null)
        {
            textNote.MaxTextWidth = maxTextWidth;
            textNote.MaxTextHeight = fontSizeSmall * 5.0;
            textNote.Trimming = TextTrimming.WordEllipsis;
        }

        // --- 2. CALCULATE DIMENSIONS ---
        double currentTextH = 0;
        double maxTextW = 0;
        double logoH = 0;

        if (File.Exists(logoPath))
        {
            logoH = 30 * ScaleFactor;
            currentTextH += logoH + (10 * ScaleFactor);
            maxTextW = 30 * ScaleFactor;
        }

        void Measure(FormattedText? ft, double marginBottom = 0)
        {
            if (ft == null) return;
            maxTextW = Math.Max(maxTextW, ft.Width);
            currentTextH += ft.Height + marginBottom;
        }

        double lineGap = 5 * ScaleFactor;

        Measure(textTitle, lineGap);
        Measure(textId, lineGap);
        Measure(textName, lineGap);
        Measure(textBrandModel, lineGap * 2);
        Measure(textSummary, lineGap * 2);
        Measure(textDates1, 2);
        Measure(textDates2, lineGap);
        Measure(textOwner, lineGap);
        Measure(textNote, 0);

        double contentHeight = Math.Max(currentTextH, qrSize);
        double finalHeight = contentHeight + (padding * 2);
        double finalWidth = padding + maxTextW + gap + qrSize + padding;

        int renderWidth = Width;
        int renderHeight = Height;

        // --- 3. DRAW ---
        var drawingVisual = new DrawingVisual();

        using (DrawingContext dc = drawingVisual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, renderWidth, renderHeight));
            dc.DrawRectangle(null, new Pen(Brushes.LightGray, 1 * ScaleFactor), new Rect(0, 0, renderWidth, renderHeight));

            double qrX = renderWidth - padding - qrSize;
            double qrY = (renderHeight - qrSize) / 2;

            string qrContent = $"ID: {device.Id}\nAd: {device.DeviceName}\nSahibi: {device.OwnerName}\nMarka: {device.Brand}\nModel: {device.Model}\nNot: {maintenanceNote}";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            using (var qrBitmap = qrCode.GetGraphic(20)) 
            {
                var qrImageSource = ConvertBitmapToImageSource(qrBitmap);
                dc.DrawImage(qrImageSource, new Rect(qrX, qrY, qrSize, qrSize));
            }

            double textBlockY = padding;
            if (textBlockY < padding) textBlockY = padding; 

            double cursorY = textBlockY;
            double cursorX = padding;

            if (File.Exists(logoPath))
            {
                try
                {
                    BitmapImage logoImage = new BitmapImage();
                    logoImage.BeginInit();
                    logoImage.UriSource = new Uri(logoPath);
                    logoImage.CacheOption = BitmapCacheOption.OnLoad;
                    logoImage.EndInit();

                    double logoW = logoH;
                    dc.DrawImage(logoImage, new Rect(cursorX, cursorY, logoW, logoH));

                    if (textTitle != null)
                    {
                        double titleX = cursorX + logoW + (10 * ScaleFactor);
                        double titleY = cursorY + (logoH - textTitle.Height) / 2;
                        dc.DrawText(textTitle, new Point(titleX, titleY));
                    }

                    cursorY += logoH + (10 * ScaleFactor);
                }
                catch { }
            }

            void Draw(FormattedText? ft, double marginBottom = 0)
            {
                if (ft == null) return;
                dc.DrawText(ft, new Point(cursorX, cursorY));
                cursorY += ft.Height + marginBottom;
            }

            Draw(textId, lineGap);
            Draw(textName, lineGap);
            Draw(textBrandModel, lineGap * 2);
            Draw(textSummary, lineGap * 2);
            Draw(textDates1, 2);
            Draw(textDates2, lineGap);
            Draw(textOwner, lineGap);
            Draw(textNote, 0);
        }

        RenderTargetBitmap rtb = new RenderTargetBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(drawingVisual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            encoder.Save(stream);
        }

        return fullPath;
    }

    private static void DrawText(DrawingContext dc, string text, Typeface typeface, double size, Brush brush, Point location)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        FormattedText formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush,
            VisualTreeHelper.GetDpi(new ContainerVisual()).PixelsPerDip);

        dc.DrawText(formattedText, location);
    }

    private static string GetTechSummary(Device device)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var specs = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(device.TechSpecs, options);
            
            if (specs == null) return "-";

            string Find(string[] keys) 
            {
                foreach(var k in keys) if(specs.ContainsKey(k)) return specs[k];
                return "";
            }

            string cpu = Find(new[] { "CPU Modeli", "CPU Markası", "CPU" });
            string ram = Find(new[] { "RAM Boyutu (Toplam)", "Toplam RAM Boyutu", "RAM Miktarı", "RAM" });
            string disk = Find(new[] { "Ana Depolama Cihazı Modeli", "Depolama Miktarı", "Depolama" });

            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(cpu)) parts.Add(cpu);
            if (!string.IsNullOrEmpty(ram)) parts.Add(ram);
            if (!string.IsNullOrEmpty(disk)) parts.Add(disk);

            return parts.Count > 0 ? string.Join(" / ", parts) : "Detay Yok";
        }
        catch
        {
            return "Veri Hatası";
        }
    }

    // Helper to convert System.Drawing.Bitmap to WPF ImageSource
    private static BitmapSource ConvertBitmapToImageSource(System.Drawing.Bitmap bitmap)
    {
        var bitmapData = bitmap.LockBits(
            new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            bitmap.PixelFormat);

        var bitmapSource = BitmapSource.Create(
            bitmapData.Width, bitmapData.Height,
            bitmap.HorizontalResolution, bitmap.VerticalResolution,
            PixelFormats.Bgra32, null,
            bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

        bitmap.UnlockBits(bitmapData);
        return bitmapSource;
    }
}