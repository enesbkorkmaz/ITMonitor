using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ITMonitor.View
{
    public partial class MainWindow : Window
    {
        private bool isDarkMode = true;

        public MainWindow()
        {
            InitializeComponent();
            BtnDashboard.IsChecked = true;
        }

        // Pencereyi üst bardan tutup sürüklemek için
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Tam ekrandayken sürüklenirse normal boyuta ve yuvarlak köşelere döndür
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                    MainBorder.BorderThickness = new Thickness(1);
                    MainBorder.CornerRadius = new CornerRadius(12); // Dış çerçeveyi yuvarlat
                    LeftPanelBorder.CornerRadius = new CornerRadius(11, 0, 0, 11); // Sol menüyü yuvarlat
                }
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Eğer pencere zaten tam ekransa normal boyuta al
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MainBorder.BorderThickness = new Thickness(1);
                MainBorder.CornerRadius = new CornerRadius(12); // Dış çerçeveyi yuvarlat
                LeftPanelBorder.CornerRadius = new CornerRadius(11, 0, 0, 11); // Sol menüyü yuvarlat
            }
            else
            {
                // Tam ekran yap
                this.WindowState = WindowState.Maximized;
                MainBorder.BorderThickness = new Thickness(0); // Tam ekranda sınırı gizle
                MainBorder.CornerRadius = new CornerRadius(0); // Dış çerçeveyi köşeli yap
                LeftPanelBorder.CornerRadius = new CornerRadius(0); // Sol menüyü köşeli yap
            }
        }

            private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            TextBlock themeIcon = (TextBlock)ThemeButton.Template.FindName("themeIcon", ThemeButton);

            if (isDarkMode)
            {
                // Gündüz Modu
                Application.Current.Resources["AppBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3"));
                Application.Current.Resources["AppBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBDBDB"));
                Application.Current.Resources["AppText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                Application.Current.Resources["InputBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E5E5"));
                Application.Current.Resources["LeftPanelBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                Application.Current.Resources["LogoColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0"));

                if (themeIcon != null) themeIcon.Text = "☀️";
                isDarkMode = false;
            }
            else
            {
                // Gece Modu
                Application.Current.Resources["AppBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                Application.Current.Resources["AppBorder"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                Application.Current.Resources["AppText"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBBBBB"));
                Application.Current.Resources["InputBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A2A"));
                Application.Current.Resources["LeftPanelBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161617"));
                Application.Current.Resources["LogoColor"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));

                if (themeIcon != null) themeIcon.Text = "🌙";
                isDarkMode = true;
            }
        }

        private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // 1. Veritabanındaki oturumu kapat
            using (var context = new ITMonitor.Data.AppDbContext())
            {
                var user = context.Users.FirstOrDefault(u => u.Username == AppState.CurrentUser);
                if (user != null)
                {
                    user.IsLoggedIn = false;
                    await context.SaveChangesAsync(); // Değişikliği veritabanına yaz
                }
            }

            // 2. Hafızayı temizle
            AppState.CurrentUser = "";

            // 3. Login ekranını yeniden başlat
            View.LoginView login = new View.LoginView();
            login.Show();

            // 4. Ana ekranı kapat
            this.Close();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void MenuButton_Checked(object sender, RoutedEventArgs e)
        {
            // Arayüz henüz tam yüklenmediyse hata vermemesi için güvenlik kontrolü
            if (MainContentArea == null) return;

            var selectedButton = sender as RadioButton;
            if (selectedButton == null) return;

            // Hangi butona tıklandığına göre ilgili sayfayı (UserControl) MainContentArea içine gömüyoruz
            switch (selectedButton.Name)
            {
                case "BtnDashboard":
                    MainContentArea.Content = new DashboardView();
                    break;

                case "BtnDevices":
                    MainContentArea.Content = new DevicesView();
                    break;

                case "BtnInventory":
                 
                    MainContentArea.Content = new InventoryView();
                    break;

                case "BtnSettings":
                    MainContentArea.Content = new SettingsView();
                    break;

                case "BtnReports":
                    MainContentArea.Content = new ReportsView();
                    break;
            }
        }
    }
}