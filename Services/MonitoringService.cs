using ITMonitor.Data;
using ITMonitor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ITMonitor.Services
{
    public class MonitoringResult
    {
        public bool IsSuccess { get; set; }
        public long ResponseTimeMs { get; set; }
        public string ErrorCode { get; set; } = "OK";
    }

    public class MonitoringService
    {
        // Detaylı Kontrol Yapan Metot
        public async Task<MonitoringResult> CheckDeviceDetailedAsync(Device device)
        {
            var result = new MonitoringResult();
            var sw = Stopwatch.StartNew();

            try
            {
                if (device.Method == "Ping")
                {
                    using (Ping pingSender = new Ping())
                    {
                        PingReply reply = await pingSender.SendPingAsync(device.IpOrUrl, 2000);
                        sw.Stop();

                        result.IsSuccess = reply.Status == IPStatus.Success;
                        result.ResponseTimeMs = result.IsSuccess ? reply.RoundtripTime : sw.ElapsedMilliseconds;
                        result.ErrorCode = reply.Status.ToString(); // Örn: Success, TimedOut, DestinationHostUnreachable
                    }
                }
                else if (device.Method == "HTTP")
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        string url = device.IpOrUrl;
                        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                            url = "http://" + url;

                        HttpResponseMessage response = await client.GetAsync(url);
                        sw.Stop();

                        result.IsSuccess = response.IsSuccessStatusCode;
                        result.ResponseTimeMs = sw.ElapsedMilliseconds;
                        result.ErrorCode = result.IsSuccess ? "200 OK" : $"HTTP {(int)response.StatusCode} ({response.StatusCode})";
                    }
                }
            }
            catch (TaskCanceledException)
            {
                sw.Stop();
                result.IsSuccess = false;
                result.ResponseTimeMs = sw.ElapsedMilliseconds;
                result.ErrorCode = "Timeout (Zaman Aşımı)";
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.IsSuccess = false;
                result.ResponseTimeMs = sw.ElapsedMilliseconds;
                result.ErrorCode = ex.Message.Length > 25 ? ex.Message.Substring(0, 25) + "..." : ex.Message;
            }

            return result;
        }

        // Tüm cihazları tarayıp hem Device hem DeviceLog tablosuna yazan metot
        public async Task RunAllChecksAsync(Action<string>? logCallback = null)
        {
            using (var context = new AppDbContext())
            {
                var devices = await context.Devices.ToListAsync();
                logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 🚀 Tarama başlatıldı. ({devices.Count} Cihaz)");

                foreach (var device in devices)
                {
                    logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 🔄 Kontrol: {device.Name} ({device.IpOrUrl})...");

                    var checkResult = await CheckDeviceDetailedAsync(device);

                    device.IsActive = checkResult.IsSuccess;
                    device.LastScanTime = DateTime.Now;
                    device.LastResponseTimeMs = checkResult.ResponseTimeMs;
                    device.LastErrorCode = checkResult.ErrorCode;

                    // Geçmiş grafiği için log kaydı ekle
                    context.DeviceLogs.Add(new DeviceLog
                    {
                        DeviceId = device.Id,
                        ResponseTimeMs = checkResult.ResponseTimeMs,
                        IsSuccess = checkResult.IsSuccess,
                        ErrorCode = checkResult.ErrorCode,
                        Timestamp = DateTime.Now
                    });

                    if (checkResult.IsSuccess)
                        logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ✅ BAŞARILI: {device.Name} ({checkResult.ResponseTimeMs} ms)");
                    else
                        logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ❌ HATA: {device.Name} -> {checkResult.ErrorCode}");
                }

                await context.SaveChangesAsync();
                logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 🎉 Tarama tamamlandı.");
            }
        }
        public async Task RunSingleCheckAsync(int deviceId)
        {
            using (var context = new AppDbContext())
            {
                var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
                if (device != null)
                {
                    // Cihazı yeni detaylı sistemle tara
                    var checkResult = await CheckDeviceDetailedAsync(device);

                    device.IsActive = checkResult.IsSuccess;
                    device.LastScanTime = DateTime.Now;
                    device.LastResponseTimeMs = checkResult.ResponseTimeMs;
                    device.LastErrorCode = checkResult.ErrorCode;

                    // Grafiklerin bozulmaması için tekil taramayı da geçmişe (Log) kaydet
                    context.DeviceLogs.Add(new DeviceLog
                    {
                        DeviceId = device.Id,
                        ResponseTimeMs = checkResult.ResponseTimeMs,
                        IsSuccess = checkResult.IsSuccess,
                        ErrorCode = checkResult.ErrorCode,
                        Timestamp = DateTime.Now
                    });

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}