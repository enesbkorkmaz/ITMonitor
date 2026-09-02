using ITMonitor.Data;
using ITMonitor.Models;
using ITMonitor.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ITMonitor.View
{
    public partial class DashboardView : UserControl
    {
        private DispatcherTimer? _scanTimer;
        public List<string> ChartTimeLabels { get; set; } = new List<string>();

        public DashboardView()
        {
            InitializeComponent();
            DataContext = this;
        }

        private async Task SetupTimerAsync()
        {
            // 1. Eğer halihazırda çalışan bir timer varsa durdur 
            if (_scanTimer != null)
            {
                _scanTimer.Stop();
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    var settings = await context.SystemSettings.FirstOrDefaultAsync();

                    // 2. Ayar yoksa veya Otomatik Tarama KAPALIYSA hiç başlatmadan çık
                    if (settings == null || !settings.AutoScanEnabled)
                        return;

                    // 3. Otomatik tarama AÇIKSA, süreyi veritabanından çek ve kur
                    _scanTimer = new DispatcherTimer();
                    _scanTimer.Interval = TimeSpan.FromMinutes(settings.ScanIntervalMinutes);
                    _scanTimer.Tick += async (s, e) =>
                    {
                        // Timer tetiklendiğinde arayüzdeki Taramayı Başlat metodunu çağır
                        await RunScanAsync();
                    };
                    _scanTimer.Start();
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda log ekranına düşsün
                AddLog($"[{DateTime.Now:HH:mm:ss}] ⚠️ Timer Hatası: {ex.Message}");
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Sayfa her açıldığında kalıcı hafızadaki saati ekrana yazdır
            TxtLastScan.Text = AppState.LastScanTime;

            // Veritabanındaki güncel ayarlara göre Timer'ı kur veya iptal et
            await SetupTimerAsync();

            await LoadDashboardStatsAsync();
        }

        private async void BtnStartScan_Click(object sender, RoutedEventArgs e)
        {
            await RunScanAsync();
        }

        private async Task RunScanAsync()
        {
            if (!BtnStartScan.IsEnabled) return;

            BtnStartScan.IsEnabled = false;
            BtnStartScan.Content = "⏳ Taranıyor...";
            ScanProgressBar.Visibility = Visibility.Visible;

            try
            {
                MonitoringService engine = new MonitoringService();
                await Task.Run(() => engine.RunAllChecksAsync(AddLog));

                // Hem hafızaya hem ekrana yazıyoruz
                string currentTime = DateTime.Now.ToString("HH:mm");
                AppState.LastScanTime = currentTime;
                TxtLastScan.Text = currentTime;

                await LoadDashboardStatsAsync();
            }
            catch (Exception ex)
            {
                AddLog($"[{DateTime.Now:HH:mm:ss}] ⚠️ Hata: {ex.Message}");
            }
            finally
            {
                BtnStartScan.IsEnabled = true;
                BtnStartScan.Content = "🚀 Taramayı Başlat";
                ScanProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogList.Items.Add(message);
                if (LogList.Items.Count > 0)
                    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            });
        }

        // TÜM GRAFİKLERİ VE KRİTİK LİSTEYİ BESLEYEN ANA METOT
        private async Task LoadDashboardStatsAsync()
        {
            using (var context = new AppDbContext())
            {
                var devices = await context.Devices.ToListAsync();

                int total = devices.Count;


                int active = devices.Count(d => d.IsActive && d.LastScanTime != null);
                int offline = devices.Count(d => !d.IsActive && d.LastScanTime != null);

                TxtTotalDevice.Text = total.ToString();
                TxtActiveDevice.Text = active.ToString();
                TxtOfflineDevice.Text = offline.ToString();

                // 1. KRİTİK CİHAZLAR LİSTESİ (Sadece Taranmış ve Hatalı Olanlar)
                var criticalDevices = devices.Where(d => !d.IsActive && d.LastScanTime != null).ToList();
                CriticalDevicesControl.ItemsSource = criticalDevices;

                // 2. PASTA GRAFİK (AĞ SAĞLIĞI)
                HealthPieChart.Series = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "Aktif",
                        Values = new ChartValues<int> { active },
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#2ECC71")!,
                        DataLabels = true
                    },
                    new PieSeries
                    {
                        Title = "Hatalı",
                        Values = new ChartValues<int> { offline },
                        Fill = (SolidColorBrush)new BrushConverter().ConvertFrom("#E74C3C")!,
                        DataLabels = true
                    }
                };

                // 3. ÇİZGİ GRAFİK (SON PİNG TRENLERİ)
                var recentLogs = await context.DeviceLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(10)
                    .OrderBy(l => l.Timestamp)
                    .ToListAsync();

                var pingValues = new ChartValues<long>();
                ChartTimeLabels.Clear();

                foreach (var log in recentLogs)
                {
                    pingValues.Add(log.ResponseTimeMs);
                    ChartTimeLabels.Add(log.Timestamp.ToString("HH:mm"));
                }

                PingLineChart.Series = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Ort. Gecikme (ms)",
                        Values = pingValues,
                        PointGeometrySize = 8,
                        LineSmoothness = 0.4
                    }
                };
            }
        }
    }
}