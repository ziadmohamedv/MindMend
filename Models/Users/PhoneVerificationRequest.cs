using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models.Users;

public class PhoneVerificationRequest
{
    [Required]
    [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "Please enter a valid Egyptian phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    [Required]
    [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "Please enter a valid Egyptian phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits")]
    public string Otp { get; set; } = string.Empty;
} 