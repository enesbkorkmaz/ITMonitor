using ITMonitor.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
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
            // Açılışta boş arama kelimesiyle listeyi yükle
            _ = LoadReportPreviewAsync("");
        }

        // YENİ EKLENEN: Arama Kutusu Tetikleyicisi
        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadReportPreviewAsync(TxtSearch.Text);
        }

        // GÜNCELLENEN: Arama kelimesini kabul eden ana yükleme metodu
        private async Task LoadReportPreviewAsync(string keyword = "")
        {
            using (var context = new AppDbContext())
            {
                // Sadece pasif/hatalı cihazları sorgula
                var query = context.Devices.Where(d => d.IsActive == false).AsQueryable();

                // Eğer arama kutusu boş değilse, cihaz isminde veya IP adresinde kelimeyi ara
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
                BtnExportPdf.Content = "⏳ Oluşturuluyor...";
                BtnExportPdf.IsEnabled = false;

                try
                {
                    using (var context = new AppDbContext())
                    {
                        var offlineDevices = await context.Devices
                                                          .Where(d => d.IsActive == false)
                                                          .ToListAsync();

                        // YENİ MERKEZİ SINIFIMIZI KULLANIYORUZ
                        var pdfDocument = ITMonitor.Services.PdfReportGenerator.CreatePdfDocument(offlineDevices);
                        pdfDocument.GeneratePdf(saveFileDialog.FileName);
                    }

                    CustomMessageBox.Show("Rapor başarıyla PDF olarak oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    CustomMessageBox.Show($"PDF oluşturulurken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
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

            try
            {
                // Servisimizi çağırıyoruz
                var emailService = new ITMonitor.Services.EmailService();
                var result = await emailService.SendReportAsync();

                if (result.isSuccess)
                {

                    CustomMessageBox.Show(result.message, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    CustomMessageBox.Show(result.message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                // Beklenmeyen sistem/ağ hatalarını yakalıyoruz
                CustomMessageBox.Show($"E-posta gönderilirken beklenmeyen bir hata oluştu: {ex.Message}", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // İşlem başarılı olsa da, hata verse de buton eski haline mutlaka dönecek
                BtnSendEmail.Content = "📧 E-Posta Gönder";
                BtnSendEmail.IsEnabled = true;
            }
        }
    }
}