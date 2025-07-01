using Mind_Mend.Models;
using System;

namespace Mind_Mend.DTOs
{
    public class CreateUserDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ReEnterPassword { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Gender { get; set; }
        public DateOnly BirthDate { get; set; }
        public string Email { get; set; }
    }
}
