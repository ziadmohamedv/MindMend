using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mind_Mend.Models.Users;

namespace Mind_Mend.Models.Appointments;

public class ProviderAvailability
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ProviderId { get; set; } = null!;

    [ForeignKey("ProviderId")]
    public User Provider { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public TimeSpan? StartTime { get; set; }

    public TimeSpan? EndTime { get; set; }

    [Required]
    public string Reason { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedById { get; set; } = null!;

    [ForeignKey("CreatedById")]
    public User CreatedBy { get; set; } = null!;
} 