using ITMonitor.Data;
using ITMonitor.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ITMonitor.Services
{
    public static class PdfReportGenerator
    {
        public static Document CreatePdfDocument(List<Device> offlineDevices)
        {
            // QuestPDF Lisans ayarı (Topluluk sürümü)
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre); // Marjları biraz daralttık
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial")); // Yazı boyutunu 11'den 9'a düşürdük

                    // --- BAŞLIK (HEADER) ---
                    page.Header().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("ITMonitor Ağ Durum Raporu").SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);
                            col.Item().Text($"Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    // --- İÇERİK (CONTENT) ---
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        x.Spacing(10);

                        if (offlineDevices.Count == 0)
                        {
                            x.Item().Text("Tebrikler! Ağda bağlantısı kopan cihaz bulunmamaktadır.")
                                .FontSize(11).FontColor(Colors.Green.Medium).SemiBold();
                        }
                        else
                        {
                            x.Item().Text($"Dikkat: Ağınızda bağlantısı kopan toplam {offlineDevices.Count} adet cihaz tespit edildi.")
                                .FontSize(10).FontColor(Colors.Red.Medium).SemiBold();

                            // MODERN TABLO GÖRÜNÜMÜ
                            x.Item().PaddingTop(10).Table(table =>
                            {
                                // Sütun Genişlikleri
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Cihaz Adı
                                    columns.RelativeColumn(1.5f); // Kategori
                                    columns.RelativeColumn(2); // IP/URL
                                    columns.RelativeColumn(3); // Hata Detayı
                                    columns.RelativeColumn(1.5f); // Son Tarama
                                });

                                // Tablo Başlıkları
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Cihaz Adı").FontColor(Colors.White).SemiBold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Kategori").FontColor(Colors.White).SemiBold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("IP / URL").FontColor(Colors.White).SemiBold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Hata Detayı").FontColor(Colors.White).SemiBold();
                                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Son Tarama").FontColor(Colors.White).SemiBold();
                                });

                                // Tablo Satırları (Veriler)
                                foreach (var device in offlineDevices)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(device.Name);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(device.Category);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(device.IpOrUrl);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(device.LastErrorCode).FontColor(Colors.Red.Medium);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(device.LastScanTime?.ToString("HH:mm") ?? "-");
                                }
                            });
                        }
                    });

                    // --- ALT BİLGİ (FOOTER) ---
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        }
    }
}