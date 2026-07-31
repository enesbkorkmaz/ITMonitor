using ITMonitor.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ITMonitor.View
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            LoadReportPreviewAsync();
        }


        private async void LoadReportPreviewAsync()
        {
            using (var context = new AppDbContext())
            {
                var offlineDevices = await context.Devices
                                                  .Where(d => d.IsActive == false)
                                                  .Select(d => new
                                                  {
                                                      StatusIcon = "🔴",
                                                      Name = d.Name,
                                                      IpOrUrl = d.IpOrUrl,
                                                      // BURASI DÜZELTİLDİ: LastErrorMessage yerine LastErrorCode kullanıyoruz
                                                      LastErrorMessage = string.IsNullOrEmpty(d.LastErrorCode) ? "Bağlantı Yok" : d.LastErrorCode
                                                  })
                                                  .ToListAsync();

                ReportPreviewList.ItemsSource = offlineDevices;
            }
        }

        private async void BtnExportTxt_Click(object sender, RoutedEventArgs e)
        {
            // Windows Dosya Kaydetme Penceresi
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Dosyası (*.txt)|*.txt",
                FileName = $"ITMonitor_Rapor_{System.DateTime.Now:yyyyMMdd_HHmm}.txt",
                Title = "Raporu Nereye Kaydetmek İstersiniz?"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Kullanıcı kaydet'e bastığında butonu kısa süreliğine kilitliyoruz
                BtnExportTxt.Content = "⏳ Kaydediliyor...";
                BtnExportTxt.IsEnabled = false;

                try
                {
                    using (var context = new AppDbContext())
                    {
                        // Sadece pasif (çökmüş) cihazları çekiyoruz
                        var offlineDevices = await context.Devices
                                                          .Where(d => d.IsActive == false)
                                                          .ToListAsync();

                        // StreamWriter ile dosyayı oluşturup içine yazıyoruz
                        using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                        {
                            sw.WriteLine("=========================================");
                            sw.WriteLine("📄 ITMonitor - Ağ Durum Raporu (TXT)");
                            sw.WriteLine($"Tarih: {System.DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                            sw.WriteLine("=========================================\n");

                            if (offlineDevices.Count == 0)
                            {
                                sw.WriteLine("Tebrikler! Ağda bağlantısı kopan cihaz bulunmamaktadır.");
                            }
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

                    // İşlem bitince kendi özel bildirim penceremizi kullanıyoruz
                    CustomMessageBox.Show("Rapor başarıyla TXT olarak kaydedildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.Show($"Dosya kaydedilirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // İşlem bitse de hata verse de butonu eski haline getir
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
                FileName = $"ITMonitor_Rapor_{System.DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Title = "Raporu Nereye Kaydetmek İstersiniz?"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Butonu geçici olarak kilitleyelim
                BtnExportPdf.Content = "⏳ Oluşturuluyor...";
                BtnExportPdf.IsEnabled = false;

                try
                {
                    // QuestPDF topluluk lisansı bildirimi (Ücretsiz kullanım için gereklidir)
                    QuestPDF.Settings.License = LicenseType.Community;

                    using (var context = new AppDbContext())
                    {
                        var offlineDevices = await context.Devices
                                                          .Where(d => d.IsActive == false)
                                                          .ToListAsync();

                        // PDF Belgesini Tasarlıyoruz
                        Document.Create(container =>
                        {
                            container.Page(page =>
                            {
                                page.Size(PageSizes.A4);
                                page.Margin(2, Unit.Centimetre);
                                page.PageColor(Colors.White);
                                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                                // PDF Başlığı (Header)
                                page.Header().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("ITMonitor Ağ Durum Raporu").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                        col.Item().Text($"Oluşturulma Tarihi: {System.DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
                                    });
                                });

                                // PDF İçeriği (Content)
                                page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                                {
                                    x.Spacing(15);

                                    if (offlineDevices.Count == 0)
                                    {
                                        x.Item().Text("Tebrikler! Ağda bağlantısı kopan cihaz bulunmamaktadır.")
                                            .FontSize(14).FontColor(Colors.Green.Medium);
                                    }
                                    else
                                    {
                                        x.Item().Text($"Toplam {offlineDevices.Count} cihazda sorun tespit edildi!")
                                            .FontSize(14).FontColor(Colors.Red.Medium).Bold();

                                        foreach (var device in offlineDevices)
                                        {
                                            x.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(y =>
                                            {
                                                y.Spacing(2);
                                                y.Item().Text($"Cihaz Adı: {device.Name} ({device.Category})").Bold().FontSize(12);
                                                y.Item().Text($"IP / URL: {device.IpOrUrl}");
                                                y.Item().Text($"Hata Detayı: {device.LastErrorCode}").FontColor(Colors.Red.Darken1);
                                                y.Item().Text($"Son Tarama: {device.LastScanTime}");
                                            });
                                        }
                                    }
                                });

                                // PDF Alt Bilgi (Footer) - Sayfa Numaraları
                                page.Footer().AlignCenter().Text(x =>
                                {
                                    x.Span("Sayfa ");
                                    x.CurrentPageNumber();
                                    x.Span(" / ");
                                    x.TotalPages();
                                });
                            });
                        })
                        .GeneratePdf(saveFileDialog.FileName); // PDF'i dosyaya yazdır
                    }

                    MessageBox.Show("Rapor başarıyla PDF olarak oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"PDF oluşturulurken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // İşlem bittiğinde butonu eski haline getir
                    BtnExportPdf.Content = "📄 PDF Kaydet";
                    BtnExportPdf.IsEnabled = true;
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
                    // Ekranda ortada duran rapor listesini yazıcıya gönderiyoruz
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

            // Servisimizi çağırıyoruz
            var emailService = new ITMonitor.Services.EmailService();
            var result = await emailService.SendReportAsync();

            if (result.isSuccess)
            {
                MessageBox.Show(result.message, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(result.message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            BtnSendEmail.Content = "📧 E-Posta Gönder";
            BtnSendEmail.IsEnabled = true;
        }
    }
}