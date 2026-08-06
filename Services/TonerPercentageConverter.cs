using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media; // Renkler (SolidColorBrush) için gerekli

namespace ITMonitor.Converters
{
    // 1. PROGRESS BAR'IN % KAÇ DOLACAĞINI HESAPLAYAN SINIF
    public class TonerPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Eğer gelen değer bir metinse ve içinde "Toner" kelimesi geçiyorsa
            if (value is string statusText && statusText.Contains("Toner"))
            {
                // Düzenli İfadeler (Regex) ile metnin içindeki sayıyı (örn: 65) yakala
                var match = Regex.Match(statusText, @"\d+");

                if (match.Success && double.TryParse(match.Value, out double percentage))
                {
                    return percentage; // Progress bar'a sayıyı gönder
                }
            }
            return 0.0; // Sayı bulamazsa %0 göster
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    // 2. PROGRESS BAR'IN RENGİNİ (KIRMIZI, SARI, YEŞİL) BELİRLEYEN SINIF
    public class TonerColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Eğer gelen veri metinse ve "Toner" geçiyorsa içindeki sayıyı bul
            if (value is string statusText && statusText.Contains("Toner"))
            {
                var match = Regex.Match(statusText, @"\d+");

                if (match.Success && double.TryParse(match.Value, out double percentage))
                {
                    // 🔴 Kritik Seviye (0 - 20)
                    if (percentage <= 20)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

                    // 🟡 Uyarı Seviyesi (21 - 50)
                    else if (percentage <= 50)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1C40F"));

                    // 🟢 İyi Seviye (51 - 100)
                    else
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                }
            }

            // Eğer sayı bulunamazsa veya cihaz yazıcı değilse şeffaf yap
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}