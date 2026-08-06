using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace ITMonitor.Services 
{
    public class SmartScanner
    {
        /// Verilen IP adresinin portlarını tarayarak cihaz kategorisini tahmin eder.
             public async Task<string> DetectDeviceCategoryAsync(string ipAddress)
        {
            // 1. Veritabanı Kontrolü (SQL Server, PostgreSQL, MySQL)
            if (await IsPortOpenAsync(ipAddress, 1433) ||
                await IsPortOpenAsync(ipAddress, 5432) ||
                await IsPortOpenAsync(ipAddress, 3306))
            {
                return "Veritabanı";
            }

            // 2. Windows Sunucu Kontrolü (RDP Portu)
            if (await IsPortOpenAsync(ipAddress, 3389))
            {
                return "Windows Sunucu";
            }

            // 3. Yazıcı Kontrolü (Raw Print veya LPD Portu)
            if (await IsPortOpenAsync(ipAddress, 9100) ||
                await IsPortOpenAsync(ipAddress, 515))
            {
                return "Yazıcı";
            }

            // 4. Linux veya Ağ Cihazı (Switch/Router) Kontrolü (SSH Portu)
            if (await IsPortOpenAsync(ipAddress, 22))
            {
                return "Ağ Cihazı / Linux";
            }

            // 5. Web Servisi veya Kamera Kontrolü (HTTP / HTTPS)
            if (await IsPortOpenAsync(ipAddress, 80) ||
                await IsPortOpenAsync(ipAddress, 443))
            {
                return "Web Servisi";
            }

            // 6. Hiçbir port cevap vermedi ama cihaz ping'e yanıt veriyor mu?
            if (await IsPingSuccessfulAsync(ipAddress))
            {
                return "Bilinmeyen Cihaz (Açık)";
            }

            return "Bağlantı Yok"; // Cihaz tamamen kapalı veya güvenlik duvarı her şeyi engelliyor
        }

        /// Belirli bir porta Asenkron TCP bağlantısı dener (Timeout mekanizması içerir).
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
                        // Zaman aşımı oldu, port büyük ihtimalle kapalı veya filtrelenmiş
                        return false;
                    }

                    // Eğer connectTask bittiyse (bağlandıysa) exception fırlatmaması için await'liyoruz
                    await connectTask;
                    return true;
                }
            }
            catch
            {
                // Bağlantı reddedildi (Port kapalı)
                return false;
            }
        }

        /// Klasik Ping testi (Fallback olarak kullanılır)
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