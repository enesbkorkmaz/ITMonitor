using ITMonitor.Data;
using ITMonitor.Models;
using Microsoft.EntityFrameworkCore; // Asenkron veritabanı işlemleri için zorunlu kütüphane
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ITMonitor.View
{
    /// <summary>
    /// LoginView.xaml etkileşim mantığı
    /// </summary>
    public partial class LoginView : Window
    {
        private bool isDarkMode = true; // Karanlık mod varsayılan

        public LoginView()
        {
            InitializeComponent();
        }

        // --- PENCEREYİ SÜRÜKLEME ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // --- GİRİŞ YAP BUTONU (ANİMASYONLU VE ASENKRON) ---
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Tasarımdaki gerçek isimlerinle (UserTextBox ve PassPasswordBox) verileri alıyoruz
            string username = UserTextBox.Text.Trim();
            string password = PassPasswordBox.Password;

            // Boş Alan Kontrolü
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                CustomMessageBox.Show("Lütfen kullanıcı adı ve şifrenizi giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // --- ANİMASYON / BEKLEME EKRANI BAŞLANGICI ---
            var btn = sender as Button; // Tıklanan butonu alıyoruz
            if (btn != null) btn.IsEnabled = false; // Butonu kilitle (çift tıklamayı önler)

            BtnText.Visibility = Visibility.Collapsed; // "Giriş Yap" yazısını gizle
            LoadingSpinner.Visibility = Visibility.Visible; // Dönen çemberi göster

            try
            {
                // Veritabanı Şifre Kontrolü (Arka planda donmadan yapılır)
                using (var context = new ITMonitor.Data.AppDbContext())
                {
                    // YENİ SİSTEM: Giren kişiyi Users tablosundan bul
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

                    // Kullanıcı bulunduysa VE şifresi eşleşiyorsa
                    if (user != null && user.Password == password)
                    {
                        // 1. Kullanıcıyı veritabanında "Oturum Açtı" olarak işaretle ve kaydet
                        user.IsLoggedIn = true;
                        await context.SaveChangesAsync();

                        // 2. Kullanıcı adını sistemin hafızasına (AppState) al
                        AppState.CurrentUser = user.Username;

                        // 3. Giriş Başarılı -> Ana Pencereyi Aç
                        MainWindow main = new MainWindow();
                        main.Show();

                        // 4. Login Ekranını Kapat
                        this.Close();
                    }
                    else
                    {
                        CustomMessageBox.Show("Kullanıcı adı veya şifre hatalı!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        PassPasswordBox.Clear(); // Hatalı şifre girildiğinde şifre kutusunu temizler

                        // --- HATALI GİRİŞTE BUTONU VE ANİMASYONU ESKİ HALİNE GETİR ---
                        if (btn != null) btn.IsEnabled = true;
                        BtnText.Visibility = Visibility.Visible;
                        LoadingSpinner.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (System.Exception ex)
            {
                CustomMessageBox.Show("Veritabanı bağlantı hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);

                // --- HATA DURUMUNDA BUTONU VE ANİMASYONU ESKİ HALİNE GETİR ---
                if (btn != null) btn.IsEnabled = true;
                BtnText.Visibility = Visibility.Visible;
                LoadingSpinner.Visibility = Visibility.Collapsed;
            }
        
        }

    

        // --- MİNİMİZE BUTONU ---
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // --- KAPAT BUTONU ---
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // --- TEMA DEĞİŞTİRME BUTONU ---
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            // XAML'deki şablonun içerisindeki simge elemanlarına erişiyoruz
            TextBlock themeIcon = (TextBlock)ThemeButton.Template.FindName("themeIcon", ThemeButton);
            TextBlock minIcon = (TextBlock)MinimizeButton.Template.FindName("minIcon", MinimizeButton);
            TextBlock closeIcon = (TextBlock)CloseButton.Template.FindName("closeIcon", CloseButton);

            if (isDarkMode)
            {
                // ----- GÜNDÜZ MODUNA GEÇİŞ -----
                // Global App.xaml kaynaklarını güncelliyoruz
                Application.Current.Resources["AppBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                Application.Current.Resources["AppBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBDBDB"));
                Application.Current.Resources["AppText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                Application.Current.Resources["InputBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F7"));
                Application.Current.Resources["LeftPanelBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECECEC"));

                // Sağ üst butonların hover efektleri ve renkleri
                ThemeButton.Tag = "Light";
                MinimizeButton.Tag = "Light";

                var lightText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                if (themeIcon != null) { themeIcon.Text = "☀️"; themeIcon.Foreground = lightText; }
                if (minIcon != null) minIcon.Foreground = lightText;
                if (closeIcon != null) closeIcon.Foreground = lightText;

                isDarkMode = false;
            }
            else
            {
                // ----- GECE MODUNA GEÇİŞ -----
                // Global App.xaml kaynaklarını güncelliyoruz
                Application.Current.Resources["AppBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                Application.Current.Resources["AppBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                Application.Current.Resources["AppText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBBBBB"));
                Application.Current.Resources["InputBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
                Application.Current.Resources["LeftPanelBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161617"));

                // Sağ üst butonların hover efektleri ve renkleri
                ThemeButton.Tag = null;
                MinimizeButton.Tag = null;

                var darkText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBBBBB"));
                if (themeIcon != null) { themeIcon.Text = "🌙"; themeIcon.Foreground = darkText; }
                if (minIcon != null) minIcon.Foreground = darkText;
                if (closeIcon != null) closeIcon.Foreground = darkText;

                isDarkMode = true;
            }
        }
    }
}