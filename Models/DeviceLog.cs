using System;
using System.ComponentModel.DataAnnotations;

namespace ITMonitor.Models
{
    public class DeviceLog
    {
        [Key]
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public long ResponseTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}