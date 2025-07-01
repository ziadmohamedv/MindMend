using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models.Appointments;

public class ManageProviderAvailabilityRequest
{
    [Required]
    public string ProviderId { get; set; } = null!;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    // Optional time range - if not provided, entire days will be blocked
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    [Required]
    [MinLength(5)]
    [MaxLength(200)]
    public string Reason { get; set; } = null!;
} 