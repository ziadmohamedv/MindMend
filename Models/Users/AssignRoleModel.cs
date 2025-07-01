using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models.Users
{
    public class AssignRoleModel
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string RoleName { get; set; } = null!;
    }
}
