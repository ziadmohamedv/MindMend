// using System;
// using System.Collections.Generic;
// using System.ComponentModel.DataAnnotations;

// namespace Mind_Mend.Models
// {
//     public class ChatThread
//     {
//         [Key]
//         public int Id { get; set; }

//         public int User1Id { get; set; } // Changed to int
//         public int User2Id { get; set; } // Changed to int

//         public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

//         public ICollection<Message> Messages { get; set; }
//     }
// }


// using System;
// using System.Collections.Generic;
// using System.ComponentModel.DataAnnotations;
// namespace Mind_Mend.Models
// {
//     public class ChatThread
//     {
//         [Key]
//         public int Id { get; set; }

//         public string User1Id { get; set; } // Changed to string
//         public string User2Id { get; set; } // Changed to string

//         public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

//         public ICollection<Message> Messages { get; set; }
//     }
// }


using Mind_Mend.Models.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models
{
    public class ChatThread
    {
        [Key]
        public int Id { get; set; }

        public string User1Id { get; set; }
        public virtual User User1 { get; set; } // Navigation property

        public string User2Id { get; set; }
        public virtual User User2 { get; set; } // Navigation property

    
    }
}