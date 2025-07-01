namespace Mind_Mend.Services;

public interface INotificationService
{
    Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    Task<bool> SendNotificationToMultipleUsersAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
    Task<bool> SendAppointmentReminderAsync(string fcmToken, string patientName, DateTime appointmentDate, TimeSpan startTime, string providerName);
    Task<bool> SendAppointmentDayNotificationAsync(string fcmToken, string patientName, DateTime appointmentDate, TimeSpan startTime, string providerName);
}