using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Mind_Mend.Models.Users
{
    public class User : IdentityUser
    {
        internal string Role;

        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
        public string? FullName { get; set; }
        [Required]
        public string Gender { get; set; } = null!;
        [Required]
        public DateOnly BirthDate { get; set; }

        public string? EmailVerificationToken { get; set; }

        // Firebase Cloud Messaging token for push notifications
        public string? FcmToken { get; set; }

        public ICollection<Message> SentMessages { get; set; } // Messages sent by this user
        public ICollection<Message> ReceivedMessages { get; set; } // Messages received by this user
    }
}
