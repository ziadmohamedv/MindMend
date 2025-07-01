using Mind_Mend.Models;
using Mind_Mend.Models.Users;

namespace Mind_Mend.DTOs
{
    public class UserDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
