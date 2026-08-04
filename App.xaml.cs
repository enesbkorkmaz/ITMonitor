using System.Windows;
using ITMonitor.Data;
using ITMonitor.Services;
using System.Linq;
using System; // Exception için eklendi

namespace ITMonitor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // DİKKAT: base.OnStartup(e) KODUNU BURADAN ALIP EN ALTA TAŞIDIK!

            try
            {
                // 1. Önce veritabanını oluştur ve admini ekle
                using (var context = new AppDbContext())
                {
                    context.Database.EnsureCreated();

                    if (!context.Users.Any())
                    {
                        context.Users.Add(new Models.User
                        {
                            Username = "admin",
                            Password = "admin"
                        });
                        context.SaveChanges();
                    }
                }

                // 2. Ekran açılmadan ÖNCE eski cihaz durumlarını ve logları temizle
                ResetAllDeviceStatuses();

                // 3. Otomasyon motorunu başlat
                ReportSchedulerService.Instance.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Başlangıç ayarları yapılırken bir hata oluştu:\n{ex.Message}", "Sistem Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 4. TEMİZLİK BİTTİ -> ŞİMDİ EKRANI GÖSTER
            base.OnStartup(e);
        }

        private void ResetAllDeviceStatuses()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // 1. Cihaz durumlarını sıfırla
                    var allDevices = db.Devices.ToList();
                    foreach (var device in allDevices)
                    {
                        device.IsActive = false;
                        device.LastScanTime = null; // Sarı yapar
                        device.LastErrorCode = null; // Eski hata kodunu siler
                    }

                    // 2. Geçmiş grafikleri besleyen Ping Loglarını tamamen sil!
                    var allLogs = db.DeviceLogs.ToList();
                    if (allLogs.Any())
                    {
                        db.DeviceLogs.RemoveRange(allLogs);
                    }

                    // Değişiklikleri kaydet
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Asıl hatayı (InnerException) yakalayıp ekrana yazdırıyoruz
                string gercekHata = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Hata Detayı:\n{gercekHata}", "Veritabanı Kilitli", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}