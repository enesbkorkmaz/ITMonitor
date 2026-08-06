using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;

namespace ITMonitor.Services
{
    public class SnmpHelper
    {
        /// <summary>
        /// Yazıcının IP adresine bağlanıp Siyah Toner yüzdesini çeker.
        /// </summary>
        public static async Task<string> GetPrinterTonerLevelAsync(string ipAddress)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Yazıcı standart OID kodları (Siyah Toner için)
                    var maxCapacityOid = new ObjectIdentifier("1.3.6.1.2.1.43.11.1.1.8.1.1");
                    var currentLevelOid = new ObjectIdentifier("1.3.6.1.2.1.43.11.1.1.9.1.1");

                    // SNMP v1/v2c ile yazıcıya "public" topluluğu (community) üzerinden istek atıyoruz
                    var result = Messenger.Get(VersionCode.V1,
                                               new IPEndPoint(IPAddress.Parse(ipAddress), 161),
                                               new OctetString("public"),
                                               new List<Variable> { new Variable(maxCapacityOid), new Variable(currentLevelOid) },
                                               2000); // 2 saniye zaman aşımı

                    if (result.Count >= 2)
                    {
                        // Gelen değerleri string'den integer'a çeviriyoruz
                        int maxCapacity = int.Parse(result[0].Data.ToString());
                        int currentLevel = int.Parse(result[1].Data.ToString());

                        // Eğer toner bitmişse veya okunamıyorsa bazı yazıcılar -1 veya -3 döndürür
                        if (currentLevel < 0 || maxCapacity <= 0)
                        {
                            return "Durum Bilinmiyor";
                        }

                        // Yüzde hesaplama
                        int percentage = (currentLevel * 100) / maxCapacity;
                        return $"% {percentage}";
                    }

                    return "Okunamadı";
                }
                catch
                {
                    return "Erişim Yok"; // SNMP kapalıysa veya cihaz cevap vermezse
                }
            });
        }
    }
}