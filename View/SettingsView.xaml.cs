using ITMonitor.Data;
using ITMonitor.Models;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace ITMonitor.View
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSettingsAsync();
            await LoadEmailsAsync();
        }

        // --- AYARLARI YÜKLEME ---
        private async Task LoadSettingsAsync()
        {
            using (var context = new AppDbContext())
            {
                var setting = await context.SystemSettings.FirstOrDefaultAsync();
                if (setting != null)
                {
                    // Otomatik Ağ Taraması
                    ChkAutoScan.IsChecked = setting.AutoScanEnabled;
                    TxtScanInterval.Text = setting.ScanIntervalMinutes.ToString();

                    // Otomatik Raporlama
                    ChkAutoReport.IsChecked = setting.IsAutoReportEnabled;
                    CmbScheduleType.SelectedIndex = setting.ReportScheduleType == "FixedTime" ? 1 : 0;
                    TxtIntervalHours.Text = setting.ReportIntervalHours.ToString();
                    TxtFixedTime.Text = setting.ReportFixedTime;

                    // SMTP Ayarları
                    TxtSmtpServer.Text = setting.SmtpServer;
                    TxtSmtpPort.Text = setting.SmtpPort.ToString();
                    TxtSenderEmail.Text = setting.SmtpEmail;
                    TxtSenderPassword.Password = setting.SmtpPassword;
                }
            }
        }

        // --- E-POSTA LİSTESİNİ YÜKLEME ---
        private async Task LoadEmailsAsync()
        {
            using (var context = new AppDbContext())
            {
                EmailList.ItemsSource = await context.Emails.ToListAsync();
            }
        }

        private void ChkAutoReport_Checked(object sender, RoutedEventArgs e)
        {
            if (SchedulePanel != null) SchedulePanel.IsEnabled = true;
        }

        private void ChkAutoReport_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SchedulePanel != null) SchedulePanel.IsEnabled = false;
        }

        private void CmbScheduleType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IntervalPanel == null || FixedTimePanel == null) return;

            var selectedItem = (ComboBoxItem)CmbScheduleType.SelectedItem;
            if (selectedItem.Tag.ToString() == "Interval")
            {
                IntervalPanel.Visibility = Visibility.Visible;
                FixedTimePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                IntervalPanel.Visibility = Visibility.Collapsed;
                FixedTimePanel.Visibility = Visibility.Visible;
            }
        }

        // --- TÜM AYARLARI KAYDETME ---
        private async void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new AppDbContext())
            {
                var setting = await context.SystemSettings.FirstOrDefaultAsync();
                if (setting == null)
                {
                    setting = new SystemSetting();
                    context.SystemSettings.Add(setting);
                }

                setting.AutoScanEnabled = ChkAutoScan.IsChecked ?? false;
                if (int.TryParse(TxtScanInterval.Text, out int scanInterval))
                    setting.ScanIntervalMinutes = scanInterval;

                setting.IsAutoReportEnabled = ChkAutoReport.IsChecked ?? false;
                var selectedType = (ComboBoxItem)CmbScheduleType.SelectedItem;
                setting.ReportScheduleType = selectedType.Tag.ToString()!;
                if (int.TryParse(TxtIntervalHours.Text, out int hours)) setting.ReportIntervalHours = hours;
                setting.ReportFixedTime = TxtFixedTime.Text;

                setting.SmtpServer = TxtSmtpServer.Text;
                if (int.TryParse(TxtSmtpPort.Text, out int port)) setting.SmtpPort = port;
                setting.SmtpEmail = TxtSenderEmail.Text;
                setting.SmtpPassword = TxtSenderPassword.Password;
                setting.UseSsl = true;

                await context.SaveChangesAsync();
                MessageBox.Show("Tüm ayarlar başarıyla kaydedildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // --- TEST E-POSTASI GÖNDERME ---
        private async void BtnTestEmail_Click(object sender, RoutedEventArgs e)
        {
            string smtpServer = TxtSmtpServer.Text.Trim();
            if (!int.TryParse(TxtSmtpPort.Text.Trim(), out int smtpPort)) smtpPort = 587;
            string senderEmail = TxtSenderEmail.Text.Trim();
            string senderPassword = TxtSenderPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(smtpServer) || string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                MessageBox.Show("Lütfen önce SMTP Sunucusu, Gönderici E-Posta ve Şifre alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnTestEmail.Content = "⏳ Gönderiliyor...";
            BtnTestEmail.IsEnabled = false;

            try
            {
                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(senderEmail, "ITMonitor Test");
                    mail.To.Add(senderEmail);
                    mail.Subject = "ITMonitor - SMTP Test E-Postası";
                    mail.Body = "Merhaba,\n\nBu e-posta ITMonitor uygulamanızdaki SMTP ayarlarının sorunsuz çalıştığını doğrulamak amacıyla gönderilmiştir.\n\nİyi çalışmalar.";

                    using (var smtp = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                        smtp.EnableSsl = true;
                        await smtp.SendMailAsync(mail);
                    }
                }

                MessageBox.Show($"Test e-postası başarıyla '{senderEmail}' adresine gönderildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Test e-postası gönderilirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnTestEmail.Content = "🧪 Test Et";
                BtnTestEmail.IsEnabled = true;
            }
        }

        // --- E-POSTA ALICISI EKLEME ---
        private async void BtnAddEmail_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewEmailAddress.Text) || TxtNewEmailAddress.Text == "eposta@adres.com")
            {
                MessageBox.Show("Lütfen geçerli bir e-posta adresi girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new AppDbContext())
            {
                context.Emails.Add(new Email
                {
                    Name = TxtNewEmailName.Text == "Ad Soyad" ? "" : TxtNewEmailName.Text,
                    EmailAddress = TxtNewEmailAddress.Text
                });
                await context.SaveChangesAsync();
            }

            TxtNewEmailName.Text = "Ad Soyad";
            TxtNewEmailAddress.Text = "eposta@adres.com";
            await LoadEmailsAsync();
        }

        // --- E-POSTA ALICISI SİLME ---
        private async void BtnDeleteEmail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                using (var context = new AppDbContext())
                {
                    var email = await context.Emails.FindAsync(id);
                    if (email != null)
                    {
                        context.Emails.Remove(email);
                        await context.SaveChangesAsync();
                        await LoadEmailsAsync();
                    }
                }
            }
        }

        // --- YENİ EKLENEN: GİRİŞ ŞİFRESİ DEĞİŞTİRME ---
        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string currentPass = TxtCurrentPassword.Password.Trim();
            string newPass = TxtNewPassword.Password.Trim();
            string confirmPass = TxtConfirmPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(currentPass) || string.IsNullOrWhiteSpace(newPass))
            {
                MessageBox.Show("Lütfen tüm şifre alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Yeni şifreler birbiriyle eşleşmiyor!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new AppDbContext())
            {
                var user = await context.Users.FirstOrDefaultAsync();
                if (user != null)
                {
                    if (user.Password != currentPass)
                    {
                        MessageBox.Show("Mevcut şifreniz hatalı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    user.Password = newPass;
                    await context.SaveChangesAsync();

                    TxtCurrentPassword.Clear();
                    TxtNewPassword.Clear();
                    TxtConfirmPassword.Clear();

                    MessageBox.Show("Giriş şifreniz başarıyla değiştirildi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Kullanıcı kaydı bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}