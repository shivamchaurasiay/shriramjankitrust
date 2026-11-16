namespace RamJankiNGO.Models
{
    public class UserProfileDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string MobileNu { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
