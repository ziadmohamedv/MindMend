namespace Mind_Mend.Models.Appointments;

public static class AppointmentConstants
{
    // Session Durations (in minutes)
    public const int DOCTOR_SESSION_DURATION = 30;
    public const int THERAPIST_SESSION_DURATION = 45;

    // Prices (in EGP)
    public const decimal DOCTOR_SESSION_PRICE = 500m;
    public const decimal THERAPIST_SESSION_PRICE = 400m;

    // Working Hours
    public const int WORKING_HOURS_START = 11; // 11 AM
    public const int WORKING_HOURS_END = 20;   // 8 PM

    // Working Days
    public static readonly DayOfWeek HOLIDAY = DayOfWeek.Friday;

    public static TimeSpan StartTime => new(WORKING_HOURS_START, 0, 0);
    public static TimeSpan EndTime => new(WORKING_HOURS_END, 0, 0);

    public static TimeSpan GetSessionDuration(AppointmentType type)
        => type == AppointmentType.Doctor 
            ? TimeSpan.FromMinutes(DOCTOR_SESSION_DURATION)
            : TimeSpan.FromMinutes(THERAPIST_SESSION_DURATION);

    public static decimal GetSessionPrice(AppointmentType type)
        => type == AppointmentType.Doctor 
            ? DOCTOR_SESSION_PRICE 
            : THERAPIST_SESSION_PRICE;
} 