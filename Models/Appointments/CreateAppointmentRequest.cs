using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models.Appointments;

public class CreateAppointmentRequest
{
    [Required]
    public string Notes { get; set; } = null!;

    [Required]
    public AppointmentMode Mode { get; set; }

    public CallType? CallType { get; set; }

    [Required]
    public AppointmentType Type { get; set; }

    public string? TherapistName { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }
} 