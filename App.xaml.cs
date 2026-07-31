using System.Windows;
using ITMonitor.Data;
using ITMonitor.Services;
using System.Linq; // Bunu eklemeyi unutma

namespace ITMonitor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var context = new AppDbContext())
            {
                // Veritabanını ve tabloları oluşturur
                context.Database.EnsureCreated();

                // --- YENİ EKLENEN KISIM: VARSAYILAN KULLANICI OLUŞTURMA ---
                // Eğer Users tablosunda hiç kayıt yoksa, varsayılan admini veritabanına ekle
                if (!context.Users.Any())
                {
                    context.Users.Add(new Models.User
                    {
                        Username = "admin",
                        Password = "admin" // Giriş yapabildiğini söylediğin şifre
                    });
                    context.SaveChanges();
                }
            }

            // OTOMASYON MOTORUNU BAŞLAT
            ReportSchedulerService.Instance.Start();
        }
    }
}