using Microsoft.AspNetCore.Identity;

namespace Mind_Mend.Models.Users
{
    public class UserRole : IdentityUserRole<string>
    {
        // Extended properties can be added here if needed
        public virtual User User { get; set; } = null!;
        public virtual IdentityRole Role { get; set; } = null!;

        public static implicit operator UserRole?(string? v)
        {
            throw new NotImplementedException();
        }
    }
}
