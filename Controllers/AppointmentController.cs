using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.Models.Appointments;
using Mind_Mend.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Mind_Mend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPaymentService _paymentService;
    private readonly MindMendDbContext _context;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(
        IAppointmentService appointmentService,
        IPaymentService paymentService,
        MindMendDbContext context,
        ILogger<AppointmentController> logger)
    {
        _appointmentService = appointmentService;
        _paymentService = paymentService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("book")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        // Validate the request
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (appointment, error) = await _appointmentService.BookAppointment(userId, request);
        if (error != null)
            return BadRequest(error);

        return Ok(new { 
            message = "Appointment booked successfully", 
            appointmentId = appointment!.Id,
            status = appointment.Status,
            startTime = appointment.StartTime.ToString(@"hh\:mm"),
            endTime = appointment.EndTime.ToString(@"hh\:mm"),
            price = appointment.Price,
            paymentStatus = appointment.PaymentStatus // Will be Pending by default
        });
    }

    [HttpGet("available-therapists")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetAvailableTherapists()
    {
        var therapists = await _appointmentService.GetAvailableTherapists();
        return Ok(therapists);
    }

    [HttpGet("available-slots/{providerName}/{date}")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetAvailableSlots([FromRoute] string providerName, DateTime date)
    {
        try
        {
            // Decode the provider name to handle spaces and special characters
            providerName = Uri.UnescapeDataString(providerName);
            
            var (slots, error) = await _appointmentService.GetAvailableSlots(providerName, date);
            if (error != null)
                return BadRequest(new { error = error });

            return Ok(new { slots = slots ?? new List<Dictionary<string, string>>() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost("{appointmentId}/pay")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> SelectPaymentMethod(int appointmentId, [FromBody] PaymentMethodRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        // Get appointment
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return NotFound("Appointment not found");

        // Verify the appointment belongs to the requesting user
        if (appointment.PatientId != userId)
            return Forbid();

        // Update payment method
        appointment.PaymentMethod = request.PaymentMethod;
        await _context.SaveChangesAsync();

        // If online payment is selected, return payment URL
        if (request.PaymentMethod == PaymentMethod.Online)
        {
            var patient = await _context.Users.FindAsync(userId);
            if (patient == null)
                return NotFound("Patient not found");

            // Generate a unique order ID that includes the appointment ID
            var uniqueOrderId = $"{appointmentId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            var paymentUrl = await _paymentService.CreatePaymentToken(
                amount: appointment.Price,
                orderId: uniqueOrderId,
                customerEmail: patient.Email ?? "no-email@mindmend.com",
                customerPhone: patient.PhoneNumber ?? "NA"
            );

            if (string.IsNullOrEmpty(paymentUrl))
                return StatusCode(500, "Failed to create payment URL");

            _logger.LogInformation($"Generated payment URL for appointment {appointmentId}, order ID: {uniqueOrderId}");

            return Ok(new { 
                paymentUrl, 
                paymentMethod = "online",
                orderId = uniqueOrderId
            });
        }

        // For in-clinic payment, just return confirmation
        return Ok(new { 
            message = "Payment method set to in-clinic. Please pay at the clinic before your appointment.",
            paymentMethod = "in-clinic"
        });
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingAppointments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var currentDate = DateTime.UtcNow.Date;
        var currentTime = DateTime.UtcNow.TimeOfDay;

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Where(a => (a.PatientId == userId || a.ProviderId == userId) &&
                       ((a.AppointmentDate > currentDate) || 
                        (a.AppointmentDate == currentDate && a.StartTime > currentTime)) &&
                       a.Status != AppointmentStatus.CancelledByPatient &&
                       a.Status != AppointmentStatus.CancelledByProvider)
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                Patient = new { a.Patient.FullName, a.Patient.Email, a.Patient.PhoneNumber },
                a.ProviderId,
                Provider = new { a.Provider.FullName, a.Provider.Email, a.Provider.PhoneNumber },
                a.AppointmentDate,
                StartTime = a.StartTime.ToString(@"hh\:mm"),
                EndTime = a.EndTime.ToString(@"hh\:mm"),
                a.DurationMinutes,
                a.Type,
                a.Mode,
                a.CallType,
                a.Notes,
                a.Price,
                a.Status,
                PaymentMethod = a.PaymentMethod.ToString(),
                PaymentStatus = a.PaymentStatus.ToString(),
                a.PaymentId,
                a.CreatedAt,
                a.ConfirmedAt,
                a.CancelledAt,
                a.CancellationReason
            })
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("previous")]
    public async Task<IActionResult> GetPreviousAppointments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var currentDate = DateTime.UtcNow.Date;
        var currentTime = DateTime.UtcNow.TimeOfDay;

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Where(a => (a.PatientId == userId || a.ProviderId == userId) &&
                       ((a.AppointmentDate < currentDate) || 
                        (a.AppointmentDate == currentDate && a.StartTime <= currentTime) ||
                        a.Status == AppointmentStatus.CancelledByPatient ||
                        a.Status == AppointmentStatus.CancelledByProvider ||
                        a.Status == AppointmentStatus.Completed))
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                Patient = new { a.Patient.FullName, a.Patient.Email, a.Patient.PhoneNumber },
                a.ProviderId,
                Provider = new { a.Provider.FullName, a.Provider.Email, a.Provider.PhoneNumber },
                a.AppointmentDate,
                StartTime = a.StartTime.ToString(@"hh\:mm"),
                EndTime = a.EndTime.ToString(@"hh\:mm"),
                a.DurationMinutes,
                a.Type,
                a.Mode,
                a.CallType,
                a.Notes,
                a.Price,
                a.Status,
                PaymentMethod = a.PaymentMethod.ToString(),
                PaymentStatus = a.PaymentStatus.ToString(),
                a.PaymentId,
                a.CreatedAt,
                a.ConfirmedAt,
                a.CancelledAt,
                a.CancellationReason
            })
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                Patient = new { a.Patient.FullName, a.Patient.Email, a.Patient.PhoneNumber },
                a.ProviderId,
                Provider = new { a.Provider.FullName, a.Provider.Email, a.Provider.PhoneNumber },
                a.AppointmentDate,
                StartTime = a.StartTime.ToString(@"hh\:mm"),
                EndTime = a.EndTime.ToString(@"hh\:mm"),
                a.DurationMinutes,
                a.Type,
                a.Mode,
                a.CallType,
                a.Notes,
                a.Price,
                a.Status,
                PaymentMethod = a.PaymentMethod.ToString(),
                PaymentStatus = a.PaymentStatus.ToString(),
                a.PaymentId,
                a.CreatedAt,
                a.ConfirmedAt,
                a.CancelledAt,
                a.CancellationReason
            })
            .FirstOrDefaultAsync();

        if (appointment == null)
            return NotFound("Appointment not found");

        // Check if the user has permission to view this appointment
        if (appointment.PatientId != userId && appointment.ProviderId != userId)
            return Forbid();

        return Ok(appointment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var (appointment, error) = await _appointmentService.UpdateAppointment(id, userId, request);
        if (error != null)
            return BadRequest(error);

        return Ok(appointment);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var (success, error) = await _appointmentService.DeleteAppointment(id);
        if (error != null)
            return BadRequest(error);

        return Ok(new { message = "Appointment deleted successfully" });
    }

    [HttpPost("payment-callback")]
    [HttpGet("payment-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback([FromQuery] string? id = null, [FromQuery] bool? success = null,
        [FromQuery] bool? is_voided = null, [FromQuery] bool? is_refunded = null, [FromQuery] string? merchant_order_id = null,
        [FromBody] PaymobCallback? callback = null)
    {
        _logger.LogInformation($"Received payment callback - Query Params: success={success}, merchant_order_id={merchant_order_id}, transaction_id={id}");

        try
        {
            // Handle both query parameters and JSON body
            var isSuccess = success ?? callback?.Success ?? false;
            var isVoided = is_voided ?? callback?.IsVoided ?? false;
            var isRefunded = is_refunded ?? callback?.IsRefunded ?? false;
            var transactionId = id ?? callback?.TransactionId ?? string.Empty;
            var orderId = merchant_order_id ?? callback?.MerchantOrderId ?? string.Empty;

            // Validate merchant_order_id
            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogWarning("Received callback with empty merchant_order_id");
                return BadRequest(new { message = "merchant_order_id is required" });
            }

            // Extract appointment ID from merchant_order_id (format: "{appointmentId}_{timestamp}")
            var orderIdParts = orderId.Split('_');
            if (orderIdParts.Length < 1 || !int.TryParse(orderIdParts[0], out var appointmentId))
            {
                _logger.LogWarning($"Invalid merchant_order_id format: {orderId}");
                return BadRequest(new { message = "Invalid merchant_order_id format" });
            }

            _logger.LogInformation($"Processing payment for appointment {appointmentId}, transaction {transactionId}, success={isSuccess}");

            // Get appointment
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                _logger.LogWarning($"Appointment not found: {appointmentId}");
                return NotFound(new { message = "Appointment not found" });
            }

            if (appointment.PaymentMethod != PaymentMethod.Online)
            {
                _logger.LogWarning($"Appointment {appointmentId} is not set for online payment");
                return BadRequest(new { message = "Appointment is not set for online payment" });
            }

            // Update payment ID
            appointment.PaymentId = transactionId;

            // Update payment status based on PayMob callback
            if (isSuccess && !isVoided && !isRefunded)
            {
                _logger.LogInformation($"Payment successful for appointment {appointmentId}");
                appointment.PaymentStatus = PaymentStatus.Paid;
                // Set confirmation time when payment is successful
                appointment.ConfirmedAt = DateTime.UtcNow;
            }
            else if (isRefunded)
            {
                _logger.LogInformation($"Payment refunded for appointment {appointmentId}");
                appointment.PaymentStatus = PaymentStatus.Refunded;
            }
            else
            {
                _logger.LogInformation($"Payment failed for appointment {appointmentId}");
                appointment.PaymentStatus = PaymentStatus.Failed;
            }

            await _context.SaveChangesAsync();
            
            // Redirect to frontend success/failure page
            var baseUrl = "http://localhost:3000"; // Change this to your frontend URL
            var redirectUrl = isSuccess ? $"{baseUrl}/payment/success" : $"{baseUrl}/payment/failure";
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing payment callback: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = "Error processing payment callback" });
        }
    }

    public class PaymentMethodRequest
    {
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
    }
}

// Paymob callback model
public class PaymobCallback
{
    public bool Success { get; set; }
    public bool IsVoided { get; set; }
    public bool IsRefunded { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string MerchantOrderId { get; set; } = string.Empty;
} 