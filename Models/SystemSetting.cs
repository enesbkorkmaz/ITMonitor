using System.ComponentModel.DataAnnotations;

namespace ITMonitor.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        // Zamanlayıcı Ayarları
        public bool AutoScanEnabled { get; set; } = true;
        public int ScanIntervalMinutes { get; set; } = 5;

        // Güvenlik
        public string AdminPassword { get; set; } = "admin"; // Varsayılan şifre

        // SMTP (E-Posta) Ayarları
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpEmail { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = true;

        // --- RAPORLAMA AYARLARI ---
        // Otomatik raporlama açık mı?
        public bool IsAutoReportEnabled { get; set; } = false;

        // Gönderim Tipi: "Interval" (Aralıklı) veya "FixedTime" (Sabit Saat)
        public string ReportScheduleType { get; set; } = "Interval";

        // Eğer aralıklı seçildiyse kaç saatte bir gönderilecek? 
        public int ReportIntervalHours { get; set; } = 1;

        // Eğer sabit saat seçildiyse hangi saatte gönderilecek? 
        public string ReportFixedTime { get; set; } = "17:00";
    }
}