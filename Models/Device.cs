using System;
using System.ComponentModel.DataAnnotations;

namespace ITMonitor.Models
{
    public class Device
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string IpOrUrl { get; set; } = string.Empty;

        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? Method { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastScanTime { get; set; }


        public long LastResponseTimeMs { get; set; } // Yanıt süresi (Milisaniye)
        public string? LastErrorCode { get; set; } = "OK"; // Hata Kodu / Durumu

    }
}