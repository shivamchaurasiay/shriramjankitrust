using System.ComponentModel.DataAnnotations;

namespace RamJankiNGO.Models
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number.")]
        [Display(Name = "Mobile Number")]
        public string Mobile { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+\-=\[\]{}|;:,.<>\/]).{8,50}$",
            ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter (A-Z), one lowercase letter (a-z), one numeric digit (0-9), and one special character (@, #, $, etc.).")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "City")]
        public string? City { get; set; }

        [Display(Name = "State")]
        public string? State { get; set; } = "Uttar Pradesh";

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "PIN Code")]
        public string? PinCode { get; set; }
    }
}
