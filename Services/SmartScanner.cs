using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace ITMonitor.Services
{
    public class SmartScanner
    {
        public async Task<string> DetectDeviceCategoryAsync(string ipAddress)
        {
            string targetIp = ipAddress;
            if (targetIp.Contains(":"))
            {
                targetIp = targetIp.Split(':')[0];
            }

            // 1. Veritabanı Kontrolü (SQL Server, PostgreSQL, MySQL)
            if (await IsPortOpenAsync(targetIp, 1421) ||
                await IsPortOpenAsync(targetIp, 1433) ||
                await IsPortOpenAsync(targetIp, 5432) ||
                await IsPortOpenAsync(targetIp, 3306))
            {
                return "Veritabanı";
            }

            // 2. Windows Sunucu Kontrolü (RDP Portu)
            if (await IsPortOpenAsync(targetIp, 3389))
            {
                return "Windows Sunucu";
            }

            // 3. Yazıcı Kontrolü (Raw Print veya LPD Portu)
            if (await IsPortOpenAsync(targetIp, 9100) ||
                await IsPortOpenAsync(targetIp, 515))
            {
                return "Yazıcı";
            }

            // 4. Linux veya Ağ Cihazı (Switch/Router) Kontrolü (SSH Portu)
            if (await IsPortOpenAsync(targetIp, 22))
            {
                return "Ağ Cihazı / Linux";
            }

            // 5. Web Servisi veya Kamera Kontrolü (HTTP / HTTPS)
            if (await IsPortOpenAsync(targetIp, 80) ||
                await IsPortOpenAsync(targetIp, 443))
            {
                return "Web Servisi";
            }

            // 6. Ping kontrolü
            if (await IsPingSuccessfulAsync(targetIp))
            {
                return "Bilinmeyen Cihaz (Açık)";
            }

            return "Bağlantı Yok"; 
        }

        private async Task<bool> IsPortOpenAsync(string ipAddress, int port, int timeoutMs = 2000)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Bağlantı denemesini ve zamanlayıcıyı aynı anda başlat
                    var connectTask = client.ConnectAsync(ipAddress, port);
                    var timeoutTask = Task.Delay(timeoutMs);

                    // Hangisi önce biterse onu al
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        return false;
                    }

                    await connectTask;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> IsPingSuccessfulAsync(string ipAddress)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(ipAddress, 1000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}