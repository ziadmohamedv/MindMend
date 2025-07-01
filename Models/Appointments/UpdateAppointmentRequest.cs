using System.ComponentModel.DataAnnotations;

namespace Mind_Mend.Models.Appointments;

public class UpdateAppointmentRequest
{
    public string? Notes { get; set; }

    public AppointmentMode? Mode { get; set; }

    public CallType? CallType { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public TimeSpan? StartTime { get; set; }

    public AppointmentStatus? Status { get; set; }

    public string? CancellationReason { get; set; }
} 