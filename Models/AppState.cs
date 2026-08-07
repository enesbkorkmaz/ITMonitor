using System;

namespace ITMonitor 
{
    public static class AppState
    {
      
        public static string LastScanTime { get; set; } = "-";
        public static string CurrentUser { get; set; } = "Admin";
    }
}