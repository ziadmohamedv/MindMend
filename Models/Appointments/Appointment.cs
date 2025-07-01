using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Mind_Mend.Models.Users;
using System.Text.Json.Serialization;

namespace Mind_Mend.Models.Appointments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppointmentMode
{
    Online,
    InPerson
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CallType
{
    Video,
    Audio
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppointmentStatus
{
    Upcoming,
    CancelledByPatient,
    CancelledByProvider,
    Completed,
    NoShow
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppointmentType
{
    Doctor,
    Therapist
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Online,
    InClinic
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public class Appointment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string PatientId { get; set; } = null!;
    
    [ForeignKey("PatientId")]
    public User Patient { get; set; } = null!;
    
    [Required]
    public string ProviderId { get; set; } = null!;
    
    [ForeignKey("ProviderId")]
    public User Provider { get; set; } = null!;

    [Required]
    public DateTime AppointmentDate { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }

    [Required]
    public int DurationMinutes { get; set; }
    
    [Required]
    public AppointmentType Type { get; set; }
    
    [Required]
    public AppointmentMode Mode { get; set; }
    
    public CallType? CallType { get; set; }
    
    [Required]
    public string Notes { get; set; } = null!;
    
    [Required]
    public decimal Price { get; set; }
    
    [Required]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Upcoming;
    
    public string? PaymentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ConfirmedAt { get; set; }
    
    public DateTime? CancelledAt { get; set; }
    
    public string? CancellationReason { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
}

