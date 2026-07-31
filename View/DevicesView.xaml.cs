using ITMonitor.Data;
using ITMonitor.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ITMonitor.View
{
    public partial class DevicesView : UserControl
    {
        public DevicesView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDevicesAsync();
        }

        // Cihazları çekip kategorilerine göre gruplayan metot
        private async Task LoadDevicesAsync(string keyword = "")
        {
            using (var context = new AppDbContext())
            {
                var query = context.Devices.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.ToLower();
                    query = query.Where(d => d.Name.ToLower().Contains(keyword) || d.IpOrUrl.ToLower().Contains(keyword));
                }

                // 1. Veritabanındaki eşleşen kayıtların hepsini belleğe al
                var devices = await query.ToListAsync();

                // 2. Bellekteki (List) cihazları Kategorisine göre gruplandır
                // (Eğer kullanıcının kaydettiği cihazın kategorisi boşsa 'Diğer' grubuna atar)
                var groupedDevices = devices.GroupBy(d => string.IsNullOrWhiteSpace(d.Category) ? "Diğer" : d.Category).ToList();

                // 3. İç İçe Liste kontrolümüze bu grupları bağla
                CategoryItemsControl.ItemsSource = groupedDevices;
            }
        }

        // Arama Kutusu Tipi
        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadDevicesAsync(TxtSearch.Text);
        }

        // Yeniden Tara Butonu
        private async void Rescan_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                int deviceId = (int)btn.Tag;

                btn.IsEnabled = false;
                btn.Content = "⏳ Taranıyor...";

                MonitoringService engine = new MonitoringService();
                await engine.RunSingleCheckAsync(deviceId);

                await LoadDevicesAsync(TxtSearch.Text);
            }
        }
    }
}