using ITMonitor.Data;
using ITMonitor.Models;
using ITMonitor.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

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
            await LoadCategoriesAsync(); // Dinamik kategorileri getir
            await LoadDevicesAsync();
        }

        // ================= KATEGORİLERİ DİNAMİK YÜKLEME =================
        private async Task LoadCategoriesAsync()
        {
            // Cihazlar sayfasındaki özel ikonların (Emoji) çalışması için gereken sabit liste
            var defaultCategories = new List<string>
    {
        "Ağ Cihazı (Switch/Router)",
        "Diğer",
        "Güvenlik Duvarı (Firewall)",
        "Ofis Cihazı",
        "Sunucu",
        "Veritabanı Sunucusu",
        "Web Servisi",
        "Yazıcı"
    };

            using (var context = new AppDbContext())
            {
                // Veritabanından mevcut olan farklı kategorileri çek (boş olanları atla)
                var dbCategories = await context.Devices
                                                .Select(d => d.Category)
                                                .Where(c => !string.IsNullOrWhiteSpace(c))
                                                .Distinct()
                                                .ToListAsync();

                // 1. Sabit listemizle veritabanından gelen listeyi birleştiriyoruz (Union)
                // 2. Ardından A'dan Z'ye alfabetik olarak sıralıyoruz (OrderBy)
                var allCategories = defaultCategories
                                    .Union(dbCategories)
                                    .OrderBy(c => c)
                                    .ToList();

                // Listeyi ComboBox'a bağla
                CmbCategory.ItemsSource = allCategories;

                // Kutunun içi boş kalmasın diye ilk sıradakini varsayılan yapıyoruz
                CmbCategory.SelectedIndex = 0;
            }
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

            // Artık IsEditable=True olduğu için seçilen değil, "yazılan/seçilen" Text'i doğrudan okuyoruz
            string category = string.IsNullOrWhiteSpace(CmbCategory.Text) ? "Diğer" : CmbCategory.Text.Trim();

            string method = (CmbMethod.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Ping (ICMP)";
            string description = TxtDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ipOrUrl))
            {
                CustomMessageBox.Show("Lütfen Cihaz Adı ve IP/URL alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new AppDbContext())
            {
                // --- YENİ EKLENEN: MÜKERRER IP KONTROLÜ ---
                bool ipExists = false;

                if (_selectedDeviceId == null)
                {
                    // YENİ EKLEME: Veritabanında bu IP'ye sahip herhangi bir kayıt var mı?
                    ipExists = await context.Devices.AnyAsync(d => d.IpOrUrl.ToLower() == ipOrUrl.ToLower());
                }
                else
                {
                    // GÜNCELLEME: Düzenlenen bu cihaz "hariç" başka bir cihazda bu IP kullanılmış mı?
                    ipExists = await context.Devices.AnyAsync(d => d.IpOrUrl.ToLower() == ipOrUrl.ToLower() && d.Id != _selectedDeviceId);
                }

                if (ipExists)
                {
                    CustomMessageBox.Show("Bu IP Adresi veya URL zaten envanterde kayıtlı! Lütfen farklı bir adres girin.", "Mükerrer Kayıt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return; // IP varsa kaydetme işlemini anında durdur
                }
                // ------------------------------------------

                if (_selectedDeviceId == null)
                {
                    // YENİ CİHAZ EKLEME
                    var newDevice = new Device
                    {
                        Name = name,
                        IpOrUrl = ipOrUrl,
                        Category = category,
                        Method = method,
                        Description = description,
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
                        device.Description = description;
                        CustomMessageBox.Show("Cihaz bilgileri güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                await context.SaveChangesAsync();
            }

            ClearForm();
            await LoadCategoriesAsync(); // Yeni bir kategori yazıldıysa ComboBox listesine eklenmesi için listeyi tazele
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
                        _selectedDeviceId = device.Id;
                        TxtDeviceName.Text = device.Name;
                        TxtIpOrUrl.Text = device.IpOrUrl;
                        TxtDescription.Text = device.Description;

                        // Dinamik kategori olduğu için sadece metni atamak yeterli
                        CmbCategory.Text = device.Category;

                        foreach (ComboBoxItem item in CmbMethod.Items)
                        {
                            if (item.Content.ToString() == device.Method)
                            {
                                CmbMethod.SelectedItem = item;
                                break;
                            }
                        }

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

                    if (_selectedDeviceId == deviceId)
                    {
                        ClearForm();
                    }

                    await LoadCategoriesAsync();
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
            TxtDescription.Clear();
            CmbCategory.SelectedIndex = 0;
            CmbMethod.SelectedIndex = 0;

            FormTitleText.Text = "Cihaz Detayları";
            BtnSave.Content = "Kaydet";
        }

        // ================= DIŞA/İÇE AKTARMA & OTOMATİK TANI =================
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                var devices = db.Devices.ToList();

                if (devices.Count == 0)
                {
                    CustomMessageBox.Show("Dışa aktarılacak cihaz bulunamadı!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ITMonitor Yedek Dosyası (*.itm)|*.itm",
                    Title = "Envanteri Dışa Aktar",
                    FileName = $"ITMonitor_Yedek_{DateTime.Now:yyyyMMdd_HHmmss}.itm"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(devices, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonString);
                    CustomMessageBox.Show("Veriler başarıyla dışa aktarıldı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Dışa aktarılırken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "ITMonitor Yedek Dosyası (*.itm)|*.itm",
                    Title = "Envanteri İçe Aktar"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string jsonString = File.ReadAllText(openFileDialog.FileName);
                    var importedDevices = JsonSerializer.Deserialize<List<Device>>(jsonString);

                    if (importedDevices != null && importedDevices.Count > 0)
                    {
                        using var db = new AppDbContext();
                        var existingIps = db.Devices.Select(d => d.IpOrUrl.ToLower()).ToList();
                        int addedCount = 0;

                        foreach (var device in importedDevices)
                        {
                            if (existingIps.Contains(device.IpOrUrl.ToLower()))
                            {
                                continue;
                            }

                            device.Id = 0;
                            db.Devices.Add(device);
                            addedCount++;
                        }

                        if (addedCount > 0)
                        {
                            db.SaveChanges();

                            await LoadCategoriesAsync();
                            await LoadDevicesAsync(TxtSearch.Text);

                            CustomMessageBox.Show($"{addedCount} yeni cihaz başarıyla içe aktarıldı!\n(Zaten var olan cihazlar atlandı.)", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            CustomMessageBox.Show("İçe aktarılan dosyadaki tüm cihazlar zaten envanterde mevcut. Yeni cihaz eklenmedi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        CustomMessageBox.Show("Dosya boş veya uygun formatta değil.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"İçe aktarılırken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAutoDiscover_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = TxtIpOrUrl.Text.Trim();

            if (string.IsNullOrEmpty(ipAddress))
            {
                CustomMessageBox.Show("Lütfen tarama yapmak için önce bir IP adresi veya URL girin.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnAutoDiscover.IsEnabled = false;
            BtnAutoDiscover.Content = "⏳ Taranıyor...";

            try
            {
                SmartScanner scanner = new SmartScanner();
                string detectedCategory = await scanner.DetectDeviceCategoryAsync(ipAddress);

                CmbCategory.Text = detectedCategory;

                switch (detectedCategory)
                {
                    case "Windows Sunucu":
                        CmbMethod.Text = "TCP - Uzak Masaüstü (3389)";
                        break;
                    case "Ağ Cihazı / Linux":
                        CmbMethod.Text = "TCP - SSH (22)";
                        break;
                    case "Veritabanı":
                        CmbMethod.Text = "TCP - SQL Server (1433)";
                        break;
                    case "Web Servisi":
                        CmbMethod.Text = "HTTP / HTTPS (Web)";
                        break;
                    case "Yazıcı":
                        CmbMethod.Text = "TCP - Yazıcı (9100)";
                        break;
                    default:
                        CmbMethod.Text = "Ping (ICMP)";
                        break;
                }

                if (string.IsNullOrEmpty(TxtDeviceName.Text))
                {
                    TxtDeviceName.Text = $"{detectedCategory} ({ipAddress})";
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Tarama sırasında beklenmeyen bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnAutoDiscover.IsEnabled = true;
                BtnAutoDiscover.Content = "🔍 Otomatik Tanı";
            }
        }
    }
}