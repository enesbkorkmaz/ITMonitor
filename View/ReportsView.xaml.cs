using ITMonitor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ITMonitor.View
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            _ = LoadReportPreviewAsync("");
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadReportPreviewAsync(TxtSearch.Text);
        }

        private async Task LoadReportPreviewAsync(string keyword = "")
        {
            using (var context = new AppDbContext())
            {
                var query = context.Devices.Where(d => d.IsActive == false).AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    string lowerKeyword = keyword.ToLower();
                    query = query.Where(d => d.Name.ToLower().Contains(lowerKeyword) ||
                                             d.IpOrUrl.ToLower().Contains(lowerKeyword));
                }

                var offlineDevices = await query.Select(d => new
                {
                    StatusIcon = "🔴",
                    Name = d.Name,
                    IpOrUrl = d.IpOrUrl,
                    LastErrorMessage = string.IsNullOrEmpty(d.LastErrorCode) ? "Bağlantı Yok" : d.LastErrorCode
                }).ToListAsync();

                ReportPreviewList.ItemsSource = offlineDevices;
            }
        }

        private async void BtnExportTxt_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Dosyası (*.txt)|*.txt",
                FileName = $"ITMonitor_Hatalilar_{System.DateTime.Now:yyyyMMdd_HHmm}.txt",
                Title = "Raporu Nereye Kaydetmek İstersiniz?"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                BtnExportTxt.Content = "⏳ Kaydediliyor...";
                BtnExportTxt.IsEnabled = false;

                try
                {
                    using (var context = new AppDbContext())
                    {
                        var offlineDevices = await context.Devices.Where(d => d.IsActive == false).ToListAsync();

                        using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                        {
                            sw.WriteLine("=========================================");
                            sw.WriteLine("📄 ITMonitor - Sadece Hatalı Cihazlar (TXT)");
                            sw.WriteLine($"Tarih: {System.DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                            sw.WriteLine("=========================================\n");

                            if (offlineDevices.Count == 0)
                                sw.WriteLine("Tebrikler! Ağda bağlantısı kopan cihaz bulunmamaktadır.");
                            else
                            {
                                sw.WriteLine($"Toplam {offlineDevices.Count} cihazda sorun tespit edildi:\n");
                                foreach (var device in offlineDevices)
                                {
                                    sw.WriteLine($"- Cihaz Adı   : {device.Name} ({device.Category})");
                                    sw.WriteLine($"  IP / URL    : {device.IpOrUrl}");
                                    sw.WriteLine($"  Hata Detayı : {device.LastErrorCode}");
                                    sw.WriteLine($"  Son Tarama  : {device.LastScanTime}");
                                    sw.WriteLine("-----------------------------------------");
                                }
                            }
                        }
                    }
                    CustomMessageBox.Show("TXT Raporu başarıyla kaydedildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.Show($"Dosya kaydedilirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    BtnExportTxt.Content = "📝 TXT Kaydet";
                    BtnExportTxt.IsEnabled = true;
                }
            }
        }

        private async void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Dosyası (*.pdf)|*.pdf",
                FileName = $"ITMonitor_Hatalilar_{System.DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Title = "Raporu Nereye Kaydetmek İstersiniz?"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                BtnExportPdf.Content = "⏳ Oluşturuluyor...";
                BtnExportPdf.IsEnabled = false;

                try
                {
                    using (var context = new AppDbContext())
                    {
                        var offlineDevices = await context.Devices.Where(d => d.IsActive == false).ToListAsync();
                        var pdfDocument = ITMonitor.Services.PdfReportGenerator.CreatePdfDocument(offlineDevices);
                        pdfDocument.GeneratePdf(saveFileDialog.FileName);
                    }
                    CustomMessageBox.Show("Hatalı cihazlar raporu PDF olarak oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.Show($"PDF oluşturulurken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    BtnExportPdf.Content = "📄 Hatalıları Kaydet";
                    BtnExportPdf.IsEnabled = true;
                }
            }
        }

        // ================= YENİ EKLENEN DETAYLI RAPOR (GRAFİKLİ) METODU =================
        private async void BtnExportDetailedPdf_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Dosyası (*.pdf)|*.pdf",
                FileName = $"ITMonitor_Detayli_Rapor_{System.DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Title = "Detaylı Raporu Nereye Kaydetmek İstersiniz?"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                BtnExportDetailedPdf.Content = "⏳ Analiz Ediliyor...";
                BtnExportDetailedPdf.IsEnabled = false;

                try
                {
                    // QuestPDF Topluluk Lisansı Tanımlaması (Hata vermemesi için)
                    QuestPDF.Settings.License = LicenseType.Community;

                    using (var context = new AppDbContext())
                    {
                        // TÜM cihazları çekiyoruz
                        var allDevices = await context.Devices.ToListAsync();

                        int totalCount = allDevices.Count;
                        int activeCount = allDevices.Count(d => d.IsActive);
                        int offlineCount = totalCount - activeCount;

                        var document = Document.Create(container =>
                        {
                            container.Page(page =>
                            {
                                page.Size(PageSizes.A4);
                                page.Margin(1.5f, Unit.Centimetre);
                                page.PageColor(Colors.White);
                                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                                // --- HEADER ---
                                page.Header().Element(compose =>
                                {
                                    compose.Row(row =>
                                    {
                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("ITMonitor").FontSize(24).Black().FontColor(Colors.Blue.Darken2);
                                            col.Item().Text("Sistem ve Ağ Durumu Detaylı Analiz Raporu").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken2);
                                            col.Item().Text($"Rapor Tarihi: {System.DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                                        });
                                    });
                                });

                                // --- CONTENT ---
                                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                                {
                                    // DÜZELTİLDİ: PaddingBottom komutu Text'ten önceye alındı
                                    col.Item().PaddingBottom(10).Text("1. Özet İstatistikler").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                                    // 3'LÜ BİLGİ KUTULARI
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeItem().PaddingRight(5).Background(Colors.Blue.Lighten4).Padding(10).Column(c =>
                                        {
                                            c.Item().Text("Toplam Cihaz").FontSize(11);
                                            c.Item().Text(totalCount.ToString()).FontSize(22).Bold().FontColor(Colors.Blue.Darken3);
                                        });
                                        row.RelativeItem().PaddingHorizontal(5).Background(Colors.Green.Lighten4).Padding(10).Column(c =>
                                        {
                                            c.Item().Text("Aktif Cihazlar").FontSize(11);
                                            c.Item().Text(activeCount.ToString()).FontSize(22).Bold().FontColor(Colors.Green.Darken3);
                                        });
                                        row.RelativeItem().PaddingLeft(5).Background(Colors.Red.Lighten4).Padding(10).Column(c =>
                                        {
                                            c.Item().Text("Hatalı Cihazlar").FontSize(11);
                                            c.Item().Text(offlineCount.ToString()).FontSize(22).Bold().FontColor(Colors.Red.Darken3);
                                        });
                                    });

                                    // DÜZELTİLDİ: Padding komutları Text'ten önceye alındı
                                    col.Item().PaddingTop(20).PaddingBottom(5).Text("2. Ağ Durum Grafiği (Aktif / Pasif Dağılımı)").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                                    // YATAY BAR GRAFİĞİ (STACKED BAR CHART)
                                    col.Item().Height(30).Row(row =>
                                    {
                                        if (totalCount == 0) return; // Sıfıra bölünme hatasını engellemek için

                                        if (activeCount > 0)
                                            row.RelativeItem(activeCount).Background(Colors.Green.Medium).AlignCenter().AlignMiddle().Text($"%{(activeCount * 100) / totalCount} Aktif").FontColor(Colors.White).SemiBold();

                                        if (offlineCount > 0)
                                            row.RelativeItem(offlineCount).Background(Colors.Red.Medium).AlignCenter().AlignMiddle().Text($"%{(offlineCount * 100) / totalCount} Hatalı").FontColor(Colors.White).SemiBold();
                                    });

                                    // DÜZELTİLDİ: Padding komutları Text'ten önceye alındı
                                    col.Item().PaddingTop(25).PaddingBottom(10).Text("3. Tüm Envanter Listesi").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                                    // DETAYLI TABLO
                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.ConstantColumn(50); // Durum ikonu
                                            columns.RelativeColumn(2);  // İsim
                                            columns.RelativeColumn(2);  // IP/URL
                                            columns.RelativeColumn(2);  // Kategori
                                            columns.RelativeColumn(3);  // Hata/Açıklama
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Durum").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Cihaz Adı").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("IP / URL").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Kategori").SemiBold();
                                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Son Durum / Hata").SemiBold();
                                        });

                                        foreach (var device in allDevices.OrderBy(d => d.IsActive)) // Önce hatalıları üste dizelim
                                        {
                                            var statusColor = device.IsActive ? Colors.Green.Medium : Colors.Red.Medium;
                                            var statusText = device.IsActive ? "Aktif" : "Hatalı";

                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(statusText).FontColor(statusColor).SemiBold();
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(device.Name);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(device.IpOrUrl);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(device.Category);

                                            string errorMsg = device.IsActive ? "Çevrimiçi" : (string.IsNullOrEmpty(device.LastErrorCode) ? "Bağlantı Kurulamadı" : device.LastErrorCode);
                                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(errorMsg).FontColor(device.IsActive ? Colors.Black : Colors.Red.Darken1);
                                        }
                                    });
                                });

                                // --- FOOTER ---
                                page.Footer().AlignCenter().Text(x =>
                                {
                                    x.Span("ITMonitor Otomatik Raporlama Sistemi | Sayfa ");
                                    x.CurrentPageNumber();
                                    x.Span(" / ");
                                    x.TotalPages();
                                });
                            });
                        });

                        document.GeneratePdf(saveFileDialog.FileName);
                    }

                    CustomMessageBox.Show("Detaylı grafikli rapor başarıyla oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.Show($"Detaylı PDF oluşturulurken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    BtnExportDetailedPdf.Content = "📊 Tümünü Raporla (Detaylı)";
                    BtnExportDetailedPdf.IsEnabled = true;
                }
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                BtnPrint.Content = "⏳ Yazdırılıyor...";
                BtnPrint.IsEnabled = false;

                try
                {
                    printDialog.PrintVisual(ReportPreviewList, "ITMonitor Ağ Durum Raporu");
                    MessageBox.Show("Yazdırma işlemi başarıyla tamamlandı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Yazdırma sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    BtnPrint.Content = "🖨️ Yazdır";
                    BtnPrint.IsEnabled = true;
                }
            }
        }

        private async void BtnSendEmail_Click(object sender, RoutedEventArgs e)
        {
            BtnSendEmail.Content = "⏳ Gönderiliyor...";
            BtnSendEmail.IsEnabled = false;

            try
            {
                var emailService = new ITMonitor.Services.EmailService();
                var result = await emailService.SendReportAsync();

                if (result.isSuccess)
                    CustomMessageBox.Show(result.message, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    CustomMessageBox.Show(result.message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.Show($"E-posta gönderilirken beklenmeyen bir hata oluştu: {ex.Message}", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSendEmail.Content = "📧 E-Posta Gönder";
                BtnSendEmail.IsEnabled = true;
            }
        }
    }
}