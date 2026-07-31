using ITMonitor.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ITMonitor.Services
{
    public class ReportSchedulerService
    {
        // Singleton deseni: Uygulama boyunca sadece bir tane zamanlayıcı çalışsın
        private static ReportSchedulerService? _instance;
        public static ReportSchedulerService Instance => _instance ??= new ReportSchedulerService();

        private Timer? _timer;
        private DateTime _lastSentTime = DateTime.MinValue; // Aralıklı gönderim takibi
        private DateTime _lastSentDate = DateTime.MinValue; // Sabit saat gönderimi takibi (Aynı gün 2 kez atmamak için)
        private bool _isProcessing = false; // Çakışmaları önlemek için kilit

        private ReportSchedulerService() { }

        public void Start()
        {
            // Arka planda her 1 dakikada bir kontrol yapacak sayacı başlatıyoruz
            _timer = new Timer(async (e) => await CheckAndSendReportAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private async Task CheckAndSendReportAsync()
        {
            if (_isProcessing) return; // Zaten işlem yapıyorsa yeni kontrolü atla
            _isProcessing = true;

            try
            {
                using (var context = new AppDbContext())
                {
                    var settings = await context.SystemSettings.FirstOrDefaultAsync();

                    // Ayar yoksa veya otomatik raporlama kapalıysa hiçbir şey yapma
                    if (settings == null || !settings.IsAutoReportEnabled) return;

                    bool shouldSend = false;
                    DateTime now = DateTime.Now;

                    if (settings.ReportScheduleType == "Interval")
                    {
                        // Program ilk açıldığında veya belirtilen saat kadar süre geçtiğinde gönder
                        if (_lastSentTime == DateTime.MinValue || (now - _lastSentTime).TotalHours >= settings.ReportIntervalHours)
                        {
                            shouldSend = true;
                        }
                    }
                    else if (settings.ReportScheduleType == "FixedTime")
                    {
                        // Girilen metni saate dönüştür (Örn: "17:00")
                        if (TimeSpan.TryParse(settings.ReportFixedTime, out TimeSpan fixedTime))
                        {
                            // Eğer o saat geldiyse/geçtiyse VE bugün henüz mail atılmadıysa gönder
                            if (now.TimeOfDay >= fixedTime && _lastSentDate.Date != now.Date)
                            {
                                shouldSend = true;
                            }
                        }
                    }

                    // Gönderim vakti geldiyse e-posta servisini tetikle
                    if (shouldSend)
                    {
                        var emailService = new EmailService();
                        var result = await emailService.SendReportAsync();

                        // Başarılıysa son gönderim tarihlerini güncelle
                        if (result.isSuccess)
                        {
                            _lastSentTime = now;
                            _lastSentDate = now.Date;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Arka plan işlemlerinde uygulama çökmesin diye hataları yutuyoruz veya logluyoruz
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}