using System.Windows;
using ITMonitor.Data;
using ITMonitor.Services;
using ITMonitor.View;
using System.Linq;
using System;

namespace ITMonitor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
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

                ResetAllDeviceStatuses();

                ReportSchedulerService.Instance.Start();

                Window startupWindow;

                using (var context = new AppDbContext())
                {
                    var loggedInUser = context.Users.FirstOrDefault(u => u.IsLoggedIn);

                    if (loggedInUser != null)
                    {
                        AppState.CurrentUser = loggedInUser.Username;
                        startupWindow = new MainWindow();
                    }
                    else
                    {
                        startupWindow = new LoginView();
                    }
                }

                this.MainWindow = startupWindow;
                startupWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Başlangıç ayarları yapılırken bir hata oluştu:\n{ex.Message}", "Sistem Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ResetAllDeviceStatuses()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var allDevices = db.Devices.ToList();
                    foreach (var device in allDevices)
                    {
                        device.IsActive = false;
                        device.LastScanTime = null; 
                        device.LastErrorCode = null; 
                    }

                    var allLogs = db.DeviceLogs.ToList();
                    if (allLogs.Any())
                    {
                        db.DeviceLogs.RemoveRange(allLogs);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                string gercekHata = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show($"Hata Detayı:\n{gercekHata}", "Veritabanı Kilitli", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}