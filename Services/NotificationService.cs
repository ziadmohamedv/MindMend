using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mind_Mend.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IConfiguration _configuration;
    private FirebaseMessaging? _messaging;

    public NotificationService(ILogger<NotificationService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                // You can either use a service account key file or set GOOGLE_APPLICATION_CREDENTIALS environment variable
                var serviceAccountPath = _configuration["Firebase:ServiceAccountPath"];

                if (!string.IsNullOrEmpty(serviceAccountPath) && File.Exists(serviceAccountPath))
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(serviceAccountPath)
                    });
                }
                else
                {
                    // Use default credentials (GOOGLE_APPLICATION_CREDENTIALS environment variable)
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.GetApplicationDefault()
                    });
                }
            }

            _messaging = FirebaseMessaging.DefaultInstance;
            _logger.LogInformation("Firebase initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase");
        }
    }

    public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (_messaging == null || string.IsNullOrEmpty(fcmToken))
        {
            _logger.LogWarning("Firebase not initialized or FCM token is empty");
            return false;
        }

        try
        {
            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            var response = await _messaging.SendAsync(message);
            _logger.LogInformation("Notification sent successfully. Message ID: {MessageId}", response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to token: {FcmToken}", fcmToken);
            return false;
        }
    }

    public async Task<bool> SendNotificationToMultipleUsersAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        if (_messaging == null || fcmTokens == null || !fcmTokens.Any())
        {
            _logger.LogWarning("Firebase not initialized or FCM tokens list is empty");
            return false;
        }

        try
        {
            var messages = fcmTokens.Select(token => new Message()
            {
                Token = token,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            }).ToList();

            var response = await _messaging.SendAllAsync(messages);
            _logger.LogInformation("Batch notification sent. Success: {SuccessCount}, Failure: {FailureCount}",
                response.SuccessCount, response.FailureCount);

            return response.SuccessCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send batch notifications");
            return false;
        }
    }

    public async Task<bool> SendAppointmentReminderAsync(string fcmToken, string patientName, DateTime appointmentDate, TimeSpan startTime, string providerName)
    {
        var title = "Appointment Reminder";
        var body = $"Hi {patientName}, your appointment with {providerName} is tomorrow at {startTime:hh\\:mm}. Don't forget!";

        var data = new Dictionary<string, string>
        {
            { "type", "appointment_reminder" },
            { "appointmentDate", appointmentDate.ToString("yyyy-MM-dd") },
            { "startTime", startTime.ToString(@"hh\:mm") },
            { "providerName", providerName }
        };

        return await SendNotificationAsync(fcmToken, title, body, data);
    }

    public async Task<bool> SendAppointmentDayNotificationAsync(string fcmToken, string patientName, DateTime appointmentDate, TimeSpan startTime, string providerName)
    {
        var title = "Appointment Today";
        var body = $"Hi {patientName}, your appointment with {providerName} is today at {startTime:hh\\:mm}. Please be ready!";

        var data = new Dictionary<string, string>
        {
            { "type", "appointment_today" },
            { "appointmentDate", appointmentDate.ToString("yyyy-MM-dd") },
            { "startTime", startTime.ToString(@"hh\:mm") },
            { "providerName", providerName }
        };

        return await SendNotificationAsync(fcmToken, title, body, data);
    }
}