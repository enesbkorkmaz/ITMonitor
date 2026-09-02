using ITMonitor.Data;
using ITMonitor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        public async Task<MonitoringResult> CheckDeviceDetailedAsync(Device device)
        {
            var result = new MonitoringResult();
            var sw = Stopwatch.StartNew();

            string method = device.Method ?? "Ping (ICMP)";

            try
            {
                // 1. TCP Port Kontrolü (Özel Port Desteği Eklendi)
                if (method.StartsWith("TCP"))
                {
                    int port = 80;
                    string targetIp = device.IpOrUrl;

                    if (method == "TCP - Özel Port")
                    {
                        var parts = targetIp.Split(':');
                        targetIp = parts[0];
                        if (parts.Length > 1)
                        {
                            int.TryParse(parts[1], out port);
                        }
                    }
                    else
                    {
                        int startIndex = method.IndexOf('(') + 1;
                        int endIndex = method.IndexOf(')');

                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            string portString = method.Substring(startIndex, endIndex - startIndex);
                            if (!int.TryParse(portString, out port))
                                throw new Exception("Port numarası okunamadı.");
                        }
                        else
                        {
                            throw new Exception("Geçersiz TCP formatı.");
                        }
                    }

                    using (var client = new TcpClient())
                    {
                        var connectTask = client.ConnectAsync(targetIp, port);
                        var timeoutTask = Task.Delay(2000);

                        if (await Task.WhenAny(connectTask, timeoutTask) == connectTask)
                        {
                            await connectTask;
                            sw.Stop();
                            result.IsSuccess = true;
                            result.ResponseTimeMs = sw.ElapsedMilliseconds;
                            result.ErrorCode = $"200 OK (Port {port})";
                        }
                        else
                        {
                            sw.Stop();
                            result.IsSuccess = false;
                            result.ResponseTimeMs = sw.ElapsedMilliseconds;
                            result.ErrorCode = $"Timeout (Port {port})";
                        }
                    }
                }
                // 2. HTTP / HTTPS Kontrolü (401 Yetki Gerekli Desteği Eklendi)
                else if (method.StartsWith("HTTP"))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        string url = device.IpOrUrl;
                        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                            url = "http://" + url;

                        HttpResponseMessage response = await client.GetAsync(url);
                        sw.Stop();

                        // 401 Unauthorized ise sistemin ayakta olduğunu kabul et
                        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            result.IsSuccess = true;
                            result.ResponseTimeMs = sw.ElapsedMilliseconds;
                            result.ErrorCode = response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Aktif (401 Yetki Gerekli)" : "200 OK";
                        }
                        else
                        {
                            result.IsSuccess = false;
                            result.ResponseTimeMs = sw.ElapsedMilliseconds;
                            result.ErrorCode = $"HTTP {(int)response.StatusCode} ({response.StatusCode})";
                        }
                    }
                }
                // 3. SNMP Kontrolü
                else if (method == "SNMP (v1/v2c)")
                {
                    using (Ping pingSender = new Ping())
                    {
                        PingReply reply = await pingSender.SendPingAsync(device.IpOrUrl, 1500);
                        if (reply.Status == IPStatus.Success)
                        {
                            sw.Stop();
                            result.IsSuccess = true;
                            result.ResponseTimeMs = reply.RoundtripTime;

                            string tonerLevel = await SnmpHelper.GetPrinterTonerLevelAsync(device.IpOrUrl);
                            result.ErrorCode = $"Aktif (Toner: {tonerLevel})";
                        }
                        else
                        {
                            sw.Stop();
                            result.IsSuccess = false;
                            result.ResponseTimeMs = sw.ElapsedMilliseconds;
                            result.ErrorCode = "Cihaz Kapalı";
                        }
                    }
                }
                // 4. Varsayılan Kontrol: Ping (ICMP) 
                else
                {
                    using (Ping pingSender = new Ping())
                    {
                        PingReply reply = await pingSender.SendPingAsync(device.IpOrUrl, 2000);
                        sw.Stop();

                        result.IsSuccess = reply.Status == IPStatus.Success;
                        result.ResponseTimeMs = result.IsSuccess ? reply.RoundtripTime : sw.ElapsedMilliseconds;
                        result.ErrorCode = reply.Status.ToString();
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

        public async Task RunAllChecksAsync(Action<string>? logCallback = null)
        {
            using (var context = new AppDbContext())
            {
                var devices = await context.Devices.ToListAsync();
                logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 🚀 Tarama başlatıldı. ({devices.Count} Cihaz)");

                foreach (var device in devices)
                {
                    string methodLog = device.Method ?? "Ping";
                    logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] 🔄 Kontrol: {device.Name} ({device.IpOrUrl}) [{methodLog}]...");

                    var checkResult = await CheckDeviceDetailedAsync(device);

                    device.IsActive = checkResult.IsSuccess;
                    device.LastScanTime = DateTime.Now;
                    device.LastResponseTimeMs = checkResult.ResponseTimeMs;
                    device.LastErrorCode = checkResult.ErrorCode;

                    context.DeviceLogs.Add(new DeviceLog
                    {
                        DeviceId = device.Id,
                        ResponseTimeMs = checkResult.ResponseTimeMs,
                        IsSuccess = checkResult.IsSuccess,
                        ErrorCode = checkResult.ErrorCode,
                        Timestamp = DateTime.Now
                    });

                    if (checkResult.IsSuccess)
                    {
                        string extraInfo = "";
                        if (checkResult.ErrorCode != "Success" && checkResult.ErrorCode != "OK")
                        {
                            extraInfo = $" - {checkResult.ErrorCode}";
                        }

                        logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ✅ BAŞARILI: {device.Name} ({checkResult.ResponseTimeMs} ms){extraInfo}");
                    }
                    else
                    {
                        logCallback?.Invoke($"[{DateTime.Now:HH:mm:ss}] ❌ HATA: {device.Name} -> {checkResult.ErrorCode}");
                    }
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
                    var checkResult = await CheckDeviceDetailedAsync(device);

                    device.IsActive = checkResult.IsSuccess;
                    device.LastScanTime = DateTime.Now;
                    device.LastResponseTimeMs = checkResult.ResponseTimeMs;
                    device.LastErrorCode = checkResult.ErrorCode;

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