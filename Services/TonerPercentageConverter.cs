using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media; 

namespace ITMonitor.Converters
{
    // 1. PROGRESS BAR'IN % KAÇ DOLACAĞINI HESAPLAYAN SINIF
    public class TonerPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusText && statusText.Contains("Toner"))
            {
                var match = Regex.Match(statusText, @"\d+");

                if (match.Success && double.TryParse(match.Value, out double percentage))
                {
                    return percentage; 
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
                    //  Kritik Seviye (0 - 20)
                    if (percentage <= 20)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

                    //  Uyarı Seviyesi (21 - 50)
                    else if (percentage <= 50)
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1C40F"));

                    //  İyi Seviye (51 - 100)
                    else
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                }
            }

            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}