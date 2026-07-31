using ITMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace ITMonitor.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceLog> DeviceLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Email> Emails { get; set; }

        // YENİ EKLENEN SATIR: Kullanıcılar Tablosu
        public DbSet<User> Users { get; set; }

        public AppDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=ITMonitor.db");
        }
    }
}