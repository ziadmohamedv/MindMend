using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models.Users
{
    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;

        [Required]
        public string FirstName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string Gender { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateOnly BirthDate { get; set; }
    }
}
