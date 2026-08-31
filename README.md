**Hazırlayan:** Enes Burak Korkmaz 

**Ana Konu:** BT Sistemlerinin günlük izleme yazılımı 

## 1. Giriş ve Amaç
Bu proje, kurum içi ağ sistemlerinin, sunucuların, web servislerinin ve ofis cihazlarının (yazıcılar vb.) sağlığını anlık olarak izlemek, olası kesintileri tespit etmek ve yöneticilere otomatik bildirimler sunmak amacıyla sıfırdan geliştirilen 'ITMonitor' projesidir. Başlangıçta basit bir 'ping atma' aracı olarak kurgulanan proje, kurumsal ihtiyaçlar doğrultusunda kendi veritabanını yöneten, dinamik PDF raporları çizebilen, TCP port ve SNMP taramaları yapabilen, uçtan uca (Full-Stack) profesyonel bir Masaüstü BT Otomasyon ve İzleme Yazılımına dönüştürülmüştür.
## 2. Kullanılan Teknolojiler ve Sistem Altyapısı
Uygulama Frontend (Ön Yüz), Backend (İş Mantığı ve İzleme Motoru) ve Database (Veritabanı Katmanı) olmak üzere üç ana katmandan (N-Tier Architecture) oluşmaktadır.

### 2.1. Geliştirme Ortamı ve Frontend
* **Platform:** .NET 8.0
* **Arayüz Tasarımı:** WPF (Windows Presentation Foundation) / XAML 
* **Tasarım Deseni:** MVVM (Model-View-ViewModel)
* **Görselleştirme:** LiveCharts.Wpf kütüphanesi (Halka ve çizgi grafikler)
* **Tema:** Gece ve Gündüz (Dark/Light) modlarına uyumlu, WindowChrome ile görev çubuğunu engellemeyen responsive ve çerçevesiz tasarım

### 2.2. Veritabanı Mimarisi
* **Veritabanı Motoru:** SQLite (Az yer kaplama ve taşınabilirlik için).
* **ORM:** Entity Framework Core (EF Core).
* **Güvenlik:** Oturum bilgileri session.txt yerine veritabanındaki 'IsLoggedIn' durum sütununa taşınarak yetkisiz erişimler engellenmiştir.

## 3. İzleme Motoru (Monitoring Engine) ve Arka Plan Servisleri
Asenkron (async/await) çalışan izleme motoru (MonitoringService), arayüzü dondurmadan arka planda çalışır.

* **Ping ve HTTP Kontrolleri:** IP adreslerine ICMP Ping paketleri gönderilerek gecikme ölçülür; HttpClient ile HTTP GET istekleri yollanıp hata kodları (404, 500 vb.) spesifik olarak yakalanır.
* **Akıllı Tarama (SmartScanner) ve TCP Port Keşfi:** Güvenlik duvarı engellerini aşmak için 80 (HTTP), 1433 (SQL Server), 3389 (RDP) ve 9100 (Ağ Yazıcısı) gibi spesifik TCP portları taranarak yanlış alarmlar önlenir.
* **SNMP Protokolü ve Donanım Analizi:** Lextm.SharpSnmpLib ile ağ yazıcılarının toner seviyeleri, Printer MIB (RFC 3805) OID kodları kullanılarak çekilir ve yüzdeye çevrilir.

## 4. Modüller ve Kullanım Kılavuzu

### 4.1. Dashboard (Gösterge Paneli)
* Ağın genel sağlığı hakkında saniyeler içinde özet bilgi sunar.
* **Canlı Log:** Sistem işlemlerini saniye saniye takip etmeyi sağlar.
* **Kritik Cihazlar Listesi:** Bağlantısı kopuk olan cihazları hata kodlarıyla kırmızı uyarılar şeklinde listeler.

### 4.2. Cihazlar ve Envanter Yönetimi
* Sistemdeki tüm donanımlar durum kapsülleri (Status Pills) ile listelenir.
* SNMP destekli yazıcılarda, IValueConverter destekli dinamik İlerleme Çubuğu (Progress Bar) ile toner doluluk oranı (%30 altı kırmızı vb.) gösterilir.
* Yeni cihaz ekleme, düzenleme ve silme işlemleri için arama kutusu ve DataTrigger destekli modern bir kontrol merkezidir.

### 4.3. Raporlama ve Dışa Aktarım Merkezi
* Ağ durumu TXT olarak kaydedilebilir veya WPF PrintDialog ile doğrudan yazdırılabilir.
* **QuestPDF** kütüphanesi kullanılarak yatay bar grafikleri ve renk kodlu tablolar içeren kurumsal PDF raporları saniyeler içinde oluşturulur.
* DRY (Kendini Tekrar Etme) prensibiyle oluşturulan 'PdfReportGenerator' sınıfı sayesinde manuel ve otomatik raporların birebir aynı olması sağlanır.

### 4.4. Otomasyon ve Ayarlar
* SMTP mail sunucu bilgileri yapılandırılabilir ve test edilebilir.
* Otomatik Tarama Zamanlayıcısı (Scheduler) ile belirlenen periyotlarda (örn: 15 dakikada bir) ağ taranıp, en güncel durum yetkililere PDF raporu olarak otomatik e-posta atılabilir.

## 5. Sonuç ve Gelecek Vizyonu
ITMonitor, sadece staj hedeflerini karşılamakla kalmayıp kurumsal BT departmanlarında kullanılabilecek, performanslı ve modern bir 'Full-Stack' masaüstü yazılımı olmuştur. 
Gelecek sürümlerde WMI (Windows Management Instrumentation) entegrasyonu ile CPU/RAM tüketimlerinin canlı izlenmesi, topoloji haritalarının çıkarılması ve Slack/Discord Webhook bildirimlerinin eklenmesi hedeflenmektedir.
