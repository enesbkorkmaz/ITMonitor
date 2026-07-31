using ITMonitor.Data;
using ITMonitor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ITMonitor.View
{
    public partial class InventoryView : UserControl
    {
        // Düzenleme (Edit) modunda hangi cihazın seçili olduğunu tutar. Null ise "Yeni Ekleme" modundayız demektir.
        private int? _selectedDeviceId = null;

        public InventoryView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDevicesAsync();
        }

        // ================= 1. VERİLERİ OKUMA VE ARAMA =================
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

                DeviceListView.ItemsSource = await query.ToListAsync();
            }
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadDevicesAsync(TxtSearch.Text);
        }

        // ================= 2. KAYDET / GÜNCELLE İŞLEMİ =================
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtDeviceName.Text.Trim();
            string ipOrUrl = TxtIpOrUrl.Text.Trim();
            string category = (CmbCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Diğer";
            string method = (CmbMethod.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Ping";
            string description = TxtDescription.Text.Trim(); // YENİ EKLENEN SATIR

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ipOrUrl))
            {
                CustomMessageBox.Show("Lütfen Cihaz Adı ve IP/URL alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new AppDbContext())
            {
                if (_selectedDeviceId == null)
                {
                    // YENİ CİHAZ EKLEME
                    var newDevice = new Device
                    {
                        Name = name,
                        IpOrUrl = ipOrUrl,
                        Category = category,
                        Method = method,
                        Description = description, // YENİ EKLENEN SATIR
                        IsActive = true
                    };
                    context.Devices.Add(newDevice);
                    CustomMessageBox.Show("Cihaz envantere başarıyla eklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // MEVCUT CİHAZI GÜNCELLEME
                    var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == _selectedDeviceId);
                    if (device != null)
                    {
                        device.Name = name;
                        device.IpOrUrl = ipOrUrl;
                        device.Category = category;
                        device.Method = method;
                        device.Description = description; // YENİ EKLENEN SATIR
                        CustomMessageBox.Show("Cihaz bilgileri güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                await context.SaveChangesAsync();
            }

            ClearForm();
            await LoadDevicesAsync(TxtSearch.Text);
        }

        // ================= 3. DÜZENLEME MODUNU AÇMA =================
        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                int deviceId = (int)btn.Tag;

                using (var context = new AppDbContext())
                {
                    var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
                    if (device != null)
                    {
                        // Formu cihazın bilgileriyle doldur
                        _selectedDeviceId = device.Id;
                        TxtDeviceName.Text = device.Name;
                        TxtIpOrUrl.Text = device.IpOrUrl;
                        TxtDescription.Text = device.Description; // YENİ EKLENEN SATIR

                        // Kategoriyi seç
                        foreach (ComboBoxItem item in CmbCategory.Items)
                        {
                            if (item.Content.ToString() == device.Category)
                            {
                                CmbCategory.SelectedItem = item;
                                break;
                            }
                        }

                        // Yöntemi seç
                        foreach (ComboBoxItem item in CmbMethod.Items)
                        {
                            if (item.Content.ToString() == device.Method)
                            {
                                CmbMethod.SelectedItem = item;
                                break;
                            }
                        }

                        // Formun başlığını ve butonunu "Düzenleme" moduna uygun değiştir
                        FormTitleText.Text = "✏️ Cihazı Düzenle";
                        BtnSave.Content = "Güncelle";
                    }
                }
            }
        }

        // ================= 4. SİLME İŞLEMİ =================
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                int deviceId = (int)btn.Tag;

                var result = CustomMessageBox.Show("Bu cihazı envanterden silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new AppDbContext())
                    {
                        var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == deviceId);
                        if (device != null)
                        {
                            context.Devices.Remove(device);
                            await context.SaveChangesAsync();
                        }
                    }

                    // Eğer silinen cihaz o an sağ tarafta düzenleniyorsa formu da temizle
                    if (_selectedDeviceId == deviceId)
                    {
                        ClearForm();
                    }

                    await LoadDevicesAsync(TxtSearch.Text);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _selectedDeviceId = null;
            TxtDeviceName.Clear();
            TxtIpOrUrl.Clear();
            TxtDescription.Clear(); // YENİ EKLENEN SATIR
            CmbCategory.SelectedIndex = 0;
            CmbMethod.SelectedIndex = 0;

            FormTitleText.Text = "Cihaz Detayları";
            BtnSave.Content = "Kaydet";
        }
    }
}