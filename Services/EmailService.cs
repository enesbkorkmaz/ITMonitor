using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ITMonitor.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent; 

namespace ITMonitor.Services
{
    public class EmailService
    {
        public async Task<(bool isSuccess, string message)> SendReportAsync()
        {
            try
            {
                // 1. ADIM: RAPOR ÖNCESİ SİSTEMİ GÜNCELLE (TAZE VERİ)
                try
                {
                    var monitoringService = new ITMonitor.Services.MonitoringService();
                    await monitoringService.RunAllChecksAsync();
                }
                catch (Exception scanEx)
                {
                    Console.WriteLine($"E-posta öncesi tarama yapılamadı: {scanEx.Message}");
                }

                // 2. ADIM: PDF'İN KAYDEDİLECEĞİ GEÇİCİ YOLU BELİRLE
                string tempPdfPath = Path.Combine(Path.GetTempPath(), $"ITMonitor_Rapor_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

                using (var context = new AppDbContext())
                {
                    // 3. ADIM: AYARLARI VE ALICILARI KONTROL ET
                    var settings = await context.SystemSettings.FirstOrDefaultAsync();
                    if (settings == null || string.IsNullOrWhiteSpace(settings.SmtpEmail) || string.IsNullOrWhiteSpace(settings.SmtpPassword))
                    {
                        return (false, "SMTP ayarları eksik! Lütfen Ayarlar sayfasından e-posta bilgilerinizi girin.");
                    }

                    var recipients = await context.Emails
                                                  .Where(e => !string.IsNullOrWhiteSpace(e.EmailAddress))
                                                  .ToListAsync();

                    if (!recipients.Any())
                        return (false, "Geçerli e-posta alıcısı bulunamadı! Lütfen Ayarlar sayfasından en az bir alıcı ekleyin.");

                    // 4. ADIM: GÜNCEL HATALI CİHAZLARI ÇEK VE PDF'İ OLUŞTUR
                    // Tarama yapıldığı için en güncel hatalı cihaz listesi gelecek
                    var offlineDevices = await context.Devices.Where(d => d.IsActive == false).ToListAsync();

                    // YENİ SİSTEM: Uzun QuestPDF kodları yerine merkezi PdfReportGenerator'ı çağırıyoruz
                    var pdfDocument = ITMonitor.Services.PdfReportGenerator.CreatePdfDocument(offlineDevices);
                    pdfDocument.GeneratePdf(tempPdfPath); // Oluşturulan tabloyu geçici dosyaya kaydet

                    // 5. ADIM: E-POSTA İÇERİĞİNİ HAZIRLA VE GÖNDER
                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(settings.SmtpEmail, "ITMonitor Tarama Raporu");

                        // To (Ana alıcı) alanının boş kalmaması için gönderici adresini yazıyoruz
                        mail.To.Add(settings.SmtpEmail);

                        mail.Subject = $"ITMonitor Güncel Durum Raporu - {DateTime.Now:dd.MM.yyyy HH:mm}";
                        mail.Body = $"Merhaba,\n\nITMonitor sistemi tarafından oluşturulan güncel ağ durum raporu detaylı tablo formatında (PDF) ektedir.\n\nSistemde tespit edilen hatalı cihaz sayısı: {offlineDevices.Count}\n\nİyi çalışmalar.";

                        // Veritabanındaki tüm alıcıları BCC (Gizli kopya) olarak ekle
                        foreach (var recipient in recipients)
                        {
                            if (!string.IsNullOrWhiteSpace(recipient.EmailAddress))
                            {
                                mail.Bcc.Add(recipient.EmailAddress.Trim());
                            }
                        }

                        // Hazırladığımız PDF'i e-postaya ekliyoruz
                        mail.Attachments.Add(new Attachment(tempPdfPath));

                        // SMTP ile gönderim
                        using (var smtp = new SmtpClient(settings.SmtpServer, settings.SmtpPort))
                        {
                            smtp.Credentials = new NetworkCredential(settings.SmtpEmail, settings.SmtpPassword);
                            smtp.EnableSsl = settings.UseSsl;
                            await smtp.SendMailAsync(mail);
                        }
                    }
                }

                // 6. ADIM: TEMİZLİK
                // E-posta gönderildikten sonra Windows'un Temp klasöründe çöp bırakmamak için PDF'i siliyoruz
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