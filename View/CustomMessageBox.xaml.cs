using System.Windows;
using System.Windows.Input;

namespace ITMonitor.View
{
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        // Pencereyi Sürükleme
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.OK;
            Close();
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Yes;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.No;
            Close();
        }

        // ================= STATİK KULLANIM METODU =================
        public static MessageBoxResult Show(string message, string title = "Bildirim", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            var msgBox = new CustomMessageBox();
            msgBox.TxtMessage.Text = message;
            msgBox.TxtTitle.Text = title;

            // SİMGE AYARLAMA
            switch (icon)
            {
                case MessageBoxImage.Information:
                    msgBox.TxtIcon.Text = "✅"; // Başarılı / Bilgi
                    break;
                case MessageBoxImage.Error:
                    msgBox.TxtIcon.Text = "❌"; // Hata
                    break;
                case MessageBoxImage.Warning:
                    msgBox.TxtIcon.Text = "⚠️"; // Uyarı
                    break;
                case MessageBoxImage.Question:
                    msgBox.TxtIcon.Text = "❓"; // Soru
                    break;
            }

            // BUTON DÜZENİ AYARLAMA
            switch (button)
            {
                case MessageBoxButton.OK:
                    msgBox.BtnOk.Visibility = Visibility.Visible;
                    msgBox.BtnYes.Visibility = Visibility.Collapsed;
                    msgBox.BtnNo.Visibility = Visibility.Collapsed;
                    break;
                case MessageBoxButton.YesNo:
                    msgBox.BtnOk.Visibility = Visibility.Collapsed;
                    msgBox.BtnYes.Visibility = Visibility.Visible;
                    msgBox.BtnNo.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.OKCancel:
                    msgBox.BtnOk.Visibility = Visibility.Visible;
                    msgBox.BtnYes.Visibility = Visibility.Collapsed;
                    msgBox.BtnNo.Visibility = Visibility.Visible;
                    msgBox.BtnNo.Content = "İptal";
                    break;
            }

            // Eğer ana pencere açık ise onun tam ortasında çıkar
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                msgBox.Owner = Application.Current.MainWindow;
                msgBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
        }
    }
}