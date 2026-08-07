namespace ITMonitor.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = "admin"; // Varsayılan ilk şifren
        public bool IsLoggedIn { get; set; } = false;
    }
}