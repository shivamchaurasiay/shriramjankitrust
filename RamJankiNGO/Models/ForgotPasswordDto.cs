using System.ComponentModel.DataAnnotations;

namespace RamJankiNGO.Models
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Please enter your registered Email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordWithOtpDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-digit OTP.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
        public string Otp { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please create a new password.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+\-=\[\]{}|;:,.<>\/]).{8,50}$",
            ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter (A-Z), one lowercase letter (a-z), one numeric digit (0-9), and one special character (@, #, $, etc.).")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
