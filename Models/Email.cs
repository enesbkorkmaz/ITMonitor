using System;

namespace ITMonitor.Models
{
    public class Email
    {
        public int Id { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}