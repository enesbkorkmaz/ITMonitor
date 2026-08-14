using ITMonitor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace ITMonitor.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceLog> DeviceLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<User> Users { get; set; }

        public AppDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ITMonitor");

            Directory.CreateDirectory(appDataPath);

            string dbPath = Path.Combine(appDataPath, "ITMonitor.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}