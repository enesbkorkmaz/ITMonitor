<div align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/WPF-Windows_Presentation_Foundation-0078D7?style=for-the-badge&logo=windows&logoColor=white" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" />
  <img src="https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white" />
  <img src="https://img.shields.io/badge/Entity_Framework_Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
</div>

<br/>

**Prepared by:** Enes Burak Korkmaz  
**Main Topic:** Daily monitoring software for IT Systems  

## 1. Introduction and Purpose
This project is 'ITMonitor', developed from scratch to monitor the health of internal network systems, servers, web services, and office devices (printers, etc.) in real-time, detect potential outages, and provide automatic notifications to administrators. Initially designed as a simple 'pinging' tool, the project has evolved according to corporate needs into a professional end-to-end (Full-Stack) Desktop IT Automation and Monitoring Software capable of managing its own database, drawing dynamic PDF reports, and performing TCP port and SNMP scans.

## 2. Technologies Used and System Infrastructure
The application consists of three main layers (N-Tier Architecture): Frontend, Backend (Business Logic and Monitoring Engine), and Database (Database Layer).

### 2.1. Development Environment and Frontend
* **Platform:** .NET 8.0
* **UI Design:** WPF (Windows Presentation Foundation) / XAML
* **Design Pattern:** MVVM (Model-View-ViewModel)
* **Visualization:** LiveCharts.Wpf library (Doughnut and line charts)
* **Theme:** Responsive and borderless design compatible with Night and Day (Dark/Light) modes, using WindowChrome to avoid blocking the taskbar.

### 2.2. Database Architecture
* **Database Engine:** SQLite (For minimal footprint and portability).
* **ORM:** Entity Framework Core (EF Core).
* **Security:** Unauthorized access is prevented by moving session information from a session.txt file to the 'IsLoggedIn' status column in the database.

## 3. Monitoring Engine and Background Services
The asynchronously (async/await) operating monitoring engine (MonitoringService) runs in the background without freezing the interface.

* **Ping and HTTP Checks:** Latency is measured by sending ICMP Ping packets to IP addresses; HTTP GET requests are sent via HttpClient, and specific error codes (404, 500, etc.) are caught.
* **Smart Scanning (SmartScanner) and TCP Port Discovery:** To bypass firewall blocks, specific TCP ports such as 80 (HTTP), 1433 (SQL Server), 3389 (RDP), and 9100 (Network Printer) are scanned to prevent false alarms.
* **SNMP Protocol and Hardware Analysis:** Using Lextm.SharpSnmpLib, toner levels of network printers are retrieved using Printer MIB (RFC 3805) OID codes and converted into percentages.

## 4. Modules and User Guide

### 4.1. Dashboard
* Provides summarized information about the overall health of the network in seconds.
* **Live Log:** Allows second-by-second tracking of system operations.
* **Critical Devices List:** Lists disconnected devices with their error codes as red warnings.

### 4.2. Devices and Inventory Management
* All hardware in the system is listed with status capsules (Status Pills).
* For SNMP-supported printers, the toner fill rate (e.g., red if below 30%) is displayed with an IValueConverter-supported dynamic Progress Bar.
* It is a modern control center supported by a search box and DataTrigger for adding, editing, and deleting new devices.

### 4.3. Reporting and Export Center
* Network status can be saved as TXT or printed directly with WPF PrintDialog.
* Professional PDF reports containing horizontal bar charts and color-coded tables are generated in seconds using the **QuestPDF** library.
* Thanks to the 'PdfReportGenerator' class created with the DRY (Don't Repeat Yourself) principle, manual and automatic reports are ensured to be exactly the same.

### 4.4. Automation and Settings
* SMTP mail server information can be configured and tested.
* With the Automatic Scan Scheduler, the network can be scanned at specified intervals (e.g., every 15 minutes), and the most up-to-date status can be automatically emailed to administrators as a PDF report.

### 5. Conclusion and Future Vision
ITMonitor has become a high-performance and modern 'Full-Stack' desktop software that can be used in corporate IT departments, not just meeting internship goals. 
Future versions aim to add live monitoring of CPU/RAM consumption with WMI (Windows Management Instrumentation) integration, generate topology maps, and include Slack/Discord Webhook notifications.

### Screenshots

  <img src="https://raw.githubusercontent.com/enesbkorkmaz/ITMonitor/refs/heads/master/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202026-08-31%20152740.png" />
  <img src="https://raw.githubusercontent.com/enesbkorkmaz/ITMonitor/refs/heads/master/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202026-08-31%20152658.png" />

---

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

