using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ITMonitor.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ITMonitor.Services
{
    public class EmailService
    {
        public async Task<(bool isSuccess, string message)> SendReportAsync()
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
                string tempPdfPath = Path.Combine(Path.GetTempPath(), $"ITMonitor_Rapor_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

                using (var context = new AppDbContext())
                {
                    // 1. SMTP Ayarlarını Kontrol Et
                    var settings = await context.SystemSettings.FirstOrDefaultAsync();
                    if (settings == null || string.IsNullOrWhiteSpace(settings.SmtpEmail) || string.IsNullOrWhiteSpace(settings.SmtpPassword))
                    {
                        return (false, "SMTP ayarları eksik! Lütfen Ayarlar sayfasından e-posta bilgilerinizi girin.");
                    }

                    // 2. Alıcıları Çek ve Boş Adresleri Filtrele
                    var recipients = await context.Emails
                                                  .Where(e => !string.IsNullOrWhiteSpace(e.EmailAddress))
                                                  .ToListAsync();

                    if (!recipients.Any())
                        return (false, "Geçerli e-posta alıcısı bulunamadı! Lütfen Ayarlar sayfasından en az bir alıcı ekleyin.");

                    var offlineDevices = await context.Devices.Where(d => d.IsActive == false).ToListAsync();

                    // 3. PDF Raporunu Oluştur
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));
                            page.Header().Text("ITMonitor Ağ Durum Raporu").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                            page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                            {
                                x.Spacing(10);
                                x.Item().Text($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}");
                                x.Item().Text($"Şu an ağda bağlantısı kopan cihaz sayısı: {offlineDevices.Count}").Bold();
                            });
                        });
                    }).GeneratePdf(tempPdfPath);

                    // 4. E-Postayı Hazırla
                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(settings.SmtpEmail, "ITMonitor Sistem Raporu");

                        // HATA DÜZELTME 1: To (Ana alıcı) alanının boş kalmaması için gönderici adresini yazıyoruz
                        mail.To.Add(settings.SmtpEmail);

                        mail.Subject = $"ITMonitor Güncel Durum Raporu - {DateTime.Now:dd.MM.yyyy HH:mm}";
                        mail.Body = $"Merhaba,\n\nITMonitor sistemi tarafından oluşturulan güncel ağ durum raporu PDF formatında ektedir.\n\nSistemde tespit edilen hatalı cihaz sayısı: {offlineDevices.Count}\n\nİyi çalışmalar.";

                        // HATA DÜZELTME 2: Alıcı adresini eklerken boş olmadığını garantiye alıyoruz
                        foreach (var recipient in recipients)
                        {
                            if (!string.IsNullOrWhiteSpace(recipient.EmailAddress))
                            {
                                mail.Bcc.Add(recipient.EmailAddress.Trim());
                            }
                        }

                        // PDF Ekini Ekle
                        mail.Attachments.Add(new Attachment(tempPdfPath));

                        // SMTP Bağlantısı ve Gönderim
                        using (var smtp = new SmtpClient(settings.SmtpServer, settings.SmtpPort))
                        {
                            smtp.Credentials = new NetworkCredential(settings.SmtpEmail, settings.SmtpPassword);
                            smtp.EnableSsl = settings.UseSsl;
                            await smtp.SendMailAsync(mail);
                        }
                    }
                }

                // Geçici PDF dosyasını temizle
                if (File.Exists(tempPdfPath)) File.Delete(tempPdfPath);

                return (true, "Rapor başarıyla tüm alıcılara gönderildi!");
            }
            catch (Exception ex)
            {
                return (false, $"E-Posta gönderilirken hata oluştu: {ex.Message}");
            }
        }
    }
}