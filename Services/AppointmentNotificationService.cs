using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mind_Mend.Data;
using Mind_Mend.Models.Appointments;
using Mind_Mend.Models.Users;

namespace Mind_Mend.Services;

public class AppointmentNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentNotificationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // Check every 30 minutes

    public AppointmentNotificationService(
        IServiceProvider serviceProvider,
        ILogger<AppointmentNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment Notification Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendNotifications();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Appointment Notification Service stopped");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Appointment Notification Service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
            }
        }
    }

    private async Task CheckAndSendNotifications()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MindMendDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var tomorrow = now.AddDays(1).Date;
        var today = now.Date;

        // Send day-before reminders (for appointments tomorrow)
        await SendDayBeforeReminders(context, notificationService, tomorrow);

        // Send same-day notifications (for appointments today)
        await SendSameDayNotifications(context, notificationService, today);
    }

    private async Task SendDayBeforeReminders(MindMendDbContext context, INotificationService notificationService, DateTime targetDate)
    {
        var appointments = await context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Where(a => a.AppointmentDate.Date == targetDate &&
                       a.Status == AppointmentStatus.Upcoming &&
                       !string.IsNullOrEmpty(a.Patient.FcmToken))
            .ToListAsync();

        foreach (var appointment in appointments)
        {
            try
            {
                var patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
                var providerName = $"{appointment.Provider.FirstName} {appointment.Provider.LastName}";

                var success = await notificationService.SendAppointmentReminderAsync(
                    appointment.Patient.FcmToken!,
                    patientName,
                    appointment.AppointmentDate,
                    appointment.StartTime,
                    providerName);

                if (success)
                {
                    _logger.LogInformation("Sent day-before reminder for appointment {AppointmentId} to patient {PatientId}",
                        appointment.Id, appointment.PatientId);
                }
                else
                {
                    _logger.LogWarning("Failed to send day-before reminder for appointment {AppointmentId} to patient {PatientId}",
                        appointment.Id, appointment.PatientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending day-before reminder for appointment {AppointmentId}", appointment.Id);
            }
        }
    }

    private async Task SendSameDayNotifications(MindMendDbContext context, INotificationService notificationService, DateTime targetDate)
    {
        var appointments = await context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Where(a => a.AppointmentDate.Date == targetDate &&
                       a.Status == AppointmentStatus.Upcoming &&
                       !string.IsNullOrEmpty(a.Patient.FcmToken))
            .ToListAsync();

        foreach (var appointment in appointments)
        {
            try
            {
                var patientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";
                var providerName = $"{appointment.Provider.FirstName} {appointment.Provider.LastName}";

                var success = await notificationService.SendAppointmentDayNotificationAsync(
                    appointment.Patient.FcmToken!,
                    patientName,
                    appointment.AppointmentDate,
                    appointment.StartTime,
                    providerName);

                if (success)
                {
                    _logger.LogInformation("Sent same-day notification for appointment {AppointmentId} to patient {PatientId}",
                        appointment.Id, appointment.PatientId);
                }
                else
                {
                    _logger.LogWarning("Failed to send same-day notification for appointment {AppointmentId} to patient {PatientId}",
                        appointment.Id, appointment.PatientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending same-day notification for appointment {AppointmentId}", appointment.Id);
            }
        }
    }
}