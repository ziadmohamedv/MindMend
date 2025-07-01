// using Mind_Mend.Models.Users;
// using System;
// using System.ComponentModel.DataAnnotations;
// using System.ComponentModel.DataAnnotations.Schema;

// namespace Mind_Mend.Models
// {
//     public class Message
//     {
//         [Key]
//         public int Id { get; set; }

//         [Required]
//         public string Content { get; set; }

//         public DateTime Timestamp { get; set; } = DateTime.UtcNow;

//         [Required]
//         public string SenderId { get; set; } // Changed to int

//         [ForeignKey("SenderId")]
//         public User Sender { get; set; }

//         public string ReceiverId { get; set; } // Changed to int

//         [ForeignKey("ReceiverId")]
//         public User Receiver { get; set; }

//         public bool IsRead { get; set; } = false;
//     }
// }




using Mind_Mend.Models.Users;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Mind_Mend.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public string SenderId { get; set; } // Ensure string

        [ForeignKey("SenderId")]
        public User Sender { get; set; }

        public string ReceiverId { get; set; } // Ensure string

        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }

        public bool IsRead { get; set; } = false;
        public int? ChatThreadId { get; set; }
        [ForeignKey("ChatThreadId")]
    public ChatThread ChatThread { get; set; }
    }
}