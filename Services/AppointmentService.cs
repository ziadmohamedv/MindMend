using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.Models.Appointments;
using Mind_Mend.Models.Users;
using Microsoft.Extensions.Logging;

namespace Mind_Mend.Services;

public interface IAppointmentService
{
    Task<bool> IsTimeSlotAvailable(string providerId, DateTime date, TimeSpan startTime, TimeSpan endTime);
    Task<bool> IsPatientAvailable(string patientId, DateTime date, TimeSpan startTime, TimeSpan endTime);
    Task<(Appointment? appointment, string? error)> BookAppointment(string patientId, CreateAppointmentRequest request);
    Task<IEnumerable<object>> GetAvailableTherapists();
    Task<(object? result, string? error)> GetAvailableSlots(string providerName, DateTime date);
    Task<(Appointment? appointment, User? patient, string? error)> GetAppointmentWithPatient(int appointmentId);
    Task<(Appointment? appointment, string? error)> UpdateAppointment(int appointmentId, string userId, UpdateAppointmentRequest request);
    Task<(bool success, string? error)> DeleteAppointment(int appointmentId);
    Task<IEnumerable<Appointment>> GetAppointmentsByUserId(string userId);
    Task<(Appointment? appointment, string? error)> GetAppointmentById(int appointmentId, string userId);
}

public class AppointmentService : IAppointmentService
{
    private readonly MindMendDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        MindMendDbContext context,
        UserManager<User> userManager,
        ILogger<AppointmentService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> IsTimeSlotAvailable(string providerId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var conflicts = await _context.Appointments
            .Where(a => a.ProviderId == providerId &&
                       a.AppointmentDate == date &&
                       a.Status != AppointmentStatus.CancelledByPatient &&
                       a.Status != AppointmentStatus.CancelledByProvider)
            .ToListAsync();

        return !conflicts.Any(a =>
            (startTime >= a.StartTime && startTime < a.EndTime) ||
            (endTime > a.StartTime && endTime <= a.EndTime) ||
            (startTime <= a.StartTime && endTime >= a.EndTime));
    }

    public async Task<bool> IsPatientAvailable(string patientId, DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
        var conflicts = await _context.Appointments
            .Where(a => a.PatientId == patientId &&
                       a.AppointmentDate == date &&
                       a.Status != AppointmentStatus.CancelledByPatient &&
                       a.Status != AppointmentStatus.CancelledByProvider)
            .ToListAsync();

        return !conflicts.Any(a =>
            (startTime >= a.StartTime && startTime < a.EndTime) ||
            (endTime > a.StartTime && endTime <= a.EndTime) ||
            (startTime <= a.StartTime && endTime >= a.EndTime));
    }

    public async Task<(Appointment? appointment, string? error)> BookAppointment(string patientId, CreateAppointmentRequest request)
    {
        // Validate working days
        if (request.AppointmentDate.DayOfWeek == AppointmentConstants.HOLIDAY)
            return (null, "Appointments are not available on Fridays");

        // Validate working hours
        if (request.StartTime < AppointmentConstants.StartTime || 
            request.StartTime.Add(AppointmentConstants.GetSessionDuration(request.Type)) > AppointmentConstants.EndTime)
            return (null, $"Appointments are only available between {AppointmentConstants.WORKING_HOURS_START}:00 and {AppointmentConstants.WORKING_HOURS_END}:00");

        // Validate call type for online appointments
        if (request.Mode == AppointmentMode.Online && !request.CallType.HasValue)
            return (null, "Call type is required for online appointments");

        // Get provider ID based on appointment type
        string providerId;
        AppointmentType appointmentType;

        if (string.IsNullOrEmpty(request.TherapistName))
        {
            // Get the only doctor in the system
            var doctor = await _userManager.GetUsersInRoleAsync(Roles.Doctor);
            if (!doctor.Any())
                return (null, "No doctor available in the system");
            providerId = doctor.First().Id;
            appointmentType = AppointmentType.Doctor;
        }
        else
        {
            // Find therapist by full name
            var therapists = await _userManager.GetUsersInRoleAsync(Roles.Therapist);
            var therapist = therapists.FirstOrDefault(t => t.FullName == request.TherapistName);
            
            if (therapist == null)
                return (null, "Invalid therapist name");
            
            providerId = therapist.Id;
            appointmentType = AppointmentType.Therapist;
        }

        // Calculate session duration and end time
        var sessionDuration = AppointmentConstants.GetSessionDuration(appointmentType);
        var endTime = request.StartTime.Add(sessionDuration);

        // Check provider availability
        if (!await IsTimeSlotAvailable(providerId, request.AppointmentDate, request.StartTime, endTime))
            return (null, "The selected time slot is not available for this provider");

        // Check patient availability
        if (!await IsPatientAvailable(patientId, request.AppointmentDate, request.StartTime, endTime))
            return (null, "You already have another appointment scheduled during this time");

        // Create the appointment
        var appointment = new Appointment
        {
            PatientId = patientId,
            ProviderId = providerId,
            AppointmentDate = request.AppointmentDate,
            StartTime = request.StartTime,
            EndTime = endTime,
            DurationMinutes = (int)sessionDuration.TotalMinutes,
            Type = appointmentType,
            Mode = request.Mode,
            CallType = request.CallType,
            Notes = request.Notes,
            Price = AppointmentConstants.GetSessionPrice(appointmentType),
            Status = AppointmentStatus.Upcoming,
            PaymentStatus = PaymentStatus.Pending // Default payment status
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return (appointment, null);
    }

    public async Task<IEnumerable<object>> GetAvailableTherapists()
    {
        var therapists = await _userManager.GetUsersInRoleAsync(Roles.Therapist);
        return therapists.Select(t => new
        {
            id = t.Id,
            name = t.FullName
        });
    }

    public async Task<(object? result, string? error)> GetAvailableSlots(string providerName, DateTime date)
    {
        // Check if it's a holiday
        if (date.DayOfWeek == AppointmentConstants.HOLIDAY)
            return (null, "No appointments available on Fridays");

        // Find provider by name
        _logger.LogInformation($"Looking for provider with name: {providerName}");
        var users = await _userManager.Users.Where(u => u.FullName == providerName).ToListAsync();
        _logger.LogInformation($"Found {users.Count} users matching the name");
        var provider = users.FirstOrDefault();
        
        if (provider == null)
            return (null, "Provider not found");

        // Check for availability blocks
        _logger.LogInformation($"Checking availability blocks for provider {provider.Id}");
        var availabilityBlock = await _context.ProviderAvailabilities
            .Where(a => a.ProviderId == provider.Id &&
                       date >= a.StartDate &&
                       date <= a.EndDate)
            .FirstOrDefaultAsync();

        if (availabilityBlock != null)
        {
            if (!availabilityBlock.StartTime.HasValue)
                return (null, $"Provider is not available on this date. Reason: {availabilityBlock.Reason}");
        }

        // Determine if it's a doctor or therapist
        bool isDoctor = await _userManager.IsInRoleAsync(provider, Roles.Doctor);
        var slotDuration = AppointmentConstants.GetSessionDuration(
            isDoctor ? AppointmentType.Doctor : AppointmentType.Therapist);

        // Get all booked appointments for the provider on the specified date
        var bookedSlots = await _context.Appointments
            .Where(a => a.ProviderId == provider.Id && 
                       a.AppointmentDate == date &&
                       a.Status != AppointmentStatus.CancelledByPatient &&
                       a.Status != AppointmentStatus.CancelledByProvider)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        // Generate available time slots
        var availableSlots = new List<Dictionary<string, string>>();
        _logger.LogInformation($"Generating slots from {AppointmentConstants.StartTime} to {AppointmentConstants.EndTime}");
        for (var time = AppointmentConstants.StartTime; 
             time.Add(slotDuration) <= AppointmentConstants.EndTime; 
             time = time.Add(slotDuration))
        {
            _logger.LogInformation($"Processing time slot: {time}");
            // Skip if time is within blocked period
            if (availabilityBlock?.StartTime != null && 
                availabilityBlock?.EndTime != null &&
                time >= availabilityBlock.StartTime &&
                time < availabilityBlock.EndTime)
            {
                continue;
            }

            var slotEndTime = time.Add(slotDuration);
            var isSlotAvailable = !bookedSlots.Any(slot =>
                (time >= slot.StartTime && time < slot.EndTime) ||
                (slotEndTime > slot.StartTime && slotEndTime <= slot.EndTime) ||
                (time <= slot.StartTime && slotEndTime >= slot.EndTime));

            if (isSlotAvailable)
            {
                try
                {
                    var startHours = (int)time.TotalHours;
                    var startMinutes = time.Minutes;
                    var endHours = (int)slotEndTime.TotalHours;
                    var endMinutes = slotEndTime.Minutes;

                    _logger.LogInformation($"Adding slot: {startHours:D2}:{startMinutes:D2} - {endHours:D2}:{endMinutes:D2}");

                    availableSlots.Add(new Dictionary<string, string>
                    {
                        ["startTime"] = $"{startHours:D2}:{startMinutes:D2}",
                        ["endTime"] = $"{endHours:D2}:{endMinutes:D2}"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error formatting time slot: {ex.Message}");
                    continue;
                }
            }
        }

        return (availableSlots, null);
    }

    public async Task<(Appointment? appointment, User? patient, string? error)> GetAppointmentWithPatient(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return (null, null, "Appointment not found");

        return (appointment, appointment.Patient, null);
    }

    public async Task<(Appointment? appointment, string? error)> UpdateAppointment(int appointmentId, string userId, UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return (null, "Appointment not found");

        // Check if the user has permission to update this appointment
        if (appointment.PatientId != userId && appointment.ProviderId != userId)
            return (null, "You don't have permission to update this appointment");

        // If changing date/time, validate availability
        if (request.AppointmentDate.HasValue && request.StartTime.HasValue)
        {
            var sessionDuration = AppointmentConstants.GetSessionDuration(appointment.Type);
            var endTime = request.StartTime.Value.Add(sessionDuration);

            // Check if the new time slot is available for the provider
            if (!await IsTimeSlotAvailable(appointment.ProviderId, request.AppointmentDate.Value, request.StartTime.Value, endTime))
                return (null, "The selected time slot is not available for this provider");

            // Check if the new time slot is available for the patient
            if (!await IsPatientAvailable(appointment.PatientId, request.AppointmentDate.Value, request.StartTime.Value, endTime))
                return (null, "Patient already has another appointment during this time");

            appointment.AppointmentDate = request.AppointmentDate.Value;
            appointment.StartTime = request.StartTime.Value;
            appointment.EndTime = endTime;
        }

        // Update other fields if provided
        if (request.Notes != null)
            appointment.Notes = request.Notes;
            
        if (request.Mode.HasValue)
            appointment.Mode = request.Mode.Value;
            
        if (request.CallType.HasValue)
            appointment.CallType = request.CallType;

        // Handle status changes
        if (request.Status.HasValue && appointment.Status != request.Status.Value)
        {
            appointment.Status = request.Status.Value;
            if (request.Status.Value == AppointmentStatus.CancelledByPatient || 
                request.Status.Value == AppointmentStatus.CancelledByProvider)
            {
                appointment.CancelledAt = DateTime.UtcNow;
                appointment.CancellationReason = request.CancellationReason;
            }
        }

        await _context.SaveChangesAsync();
        return (appointment, null);
    }

    public async Task<(bool success, string? error)> DeleteAppointment(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        
        if (appointment == null)
            return (false, "Appointment not found");

        // Check if the appointment is already completed
        if (appointment.Status == AppointmentStatus.Completed)
            return (false, "Cannot delete completed appointments");

        // If the appointment has a payment, we should handle refund logic here
        if (!string.IsNullOrEmpty(appointment.PaymentId))
        {
            // TODO: Implement refund logic if needed
            // For now, we'll just log that there was a payment
            _logger.LogInformation($"Appointment {appointmentId} was deleted with payment ID {appointment.PaymentId}");
        }

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        
        return (true, null);
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByUserId(string userId)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Where(a => a.PatientId == userId || a.ProviderId == userId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToListAsync();
    }

    public async Task<(Appointment? appointment, string? error)> GetAppointmentById(int appointmentId, string userId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return (null, "Appointment not found");

        // Check if the user has permission to view this appointment
        if (appointment.PatientId != userId && appointment.ProviderId != userId)
            return (null, "You don't have permission to view this appointment");

        return (appointment, null);
    }
} 