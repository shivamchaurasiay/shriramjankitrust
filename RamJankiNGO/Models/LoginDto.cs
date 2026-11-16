using System.ComponentModel.DataAnnotations;

namespace RamJankiNGO.Models
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Mobile number or Email is required.")]
        public string Mobile { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
