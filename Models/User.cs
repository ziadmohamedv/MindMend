//using System.ComponentModel.DataAnnotations;

//namespace Mind_Mend.Models
//{
//    public enum UserRole
//    {
//        Patient,
//        Therapist,
//        Doctor,
//        Admin
//        //Admin = 0,
//        //Patient = 1,
//        //Therapist = 2,
//        //Doctor = 3
//    }

//    public enum Gender
//    {
//        Male,
//        Female,
//        Other
//    }

//    public class User
//    {
//        [Key]
//        public int Id { get; set; }

//        [Required]
//        public string Username { get; set; }

//        [Required]
//        public string Password { get; set; }

//        [Required]
//        public UserRole Role { get; set; }


//        public string FullName { get; set; }

//        public string FirstName { get; set; }

//        public string LastName { get; set; }

//        public Gender Gender { get; set; }

//        public DateOnly BirthDate { get; set; }

//        public string Email { get; set; }

//        public bool EmailConfirmed { get; set; }

//        public string? EmailVerificationToken { get; set; }

//        // Navigation properties for Message relationships
//        public ICollection<Message> SentMessages { get; set; } // Messages sent by this user
//        public ICollection<Message> ReceivedMessages { get; set; } // Messages received by this user
//    }
//}


