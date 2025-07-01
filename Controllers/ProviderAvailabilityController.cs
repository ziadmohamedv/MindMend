using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.Models.Appointments;
using System.Security.Claims;

namespace Mind_Mend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ProviderAvailabilityController : ControllerBase
{
    private readonly MindMendDbContext _context;

    public ProviderAvailabilityController(MindMendDbContext context)
    {
        _context = context;
    }

    [HttpPost("block")]
    public async Task<IActionResult> BlockProviderAvailability([FromBody] ManageProviderAvailabilityRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate date range
        if (request.EndDate < request.StartDate)
            return BadRequest("End date must be after start date");

        // Validate time range if provided
        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            if (request.EndTime.Value <= request.StartTime.Value)
                return BadRequest("End time must be after start time");
        }

        // Check if provider exists
        var provider = await _context.Users.FindAsync(request.ProviderId);
        if (provider == null)
            return NotFound("Provider not found");

        // Check for existing appointments in the blocked period
        var existingAppointments = await _context.Appointments
            .Where(a => a.ProviderId == request.ProviderId &&
                       a.AppointmentDate >= request.StartDate.Date &&
                       a.AppointmentDate <= request.EndDate.Date &&
                       a.Status != AppointmentStatus.CancelledByPatient &&
                       a.Status != AppointmentStatus.CancelledByProvider)
            .ToListAsync();

        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            existingAppointments = existingAppointments
                .Where(a => !(a.StartTime >= request.EndTime || a.EndTime <= request.StartTime))
                .ToList();
        }

        if (existingAppointments.Any())
            return BadRequest("There are existing appointments during this period");

        // Create availability block
        var availability = new ProviderAvailability
        {
            ProviderId = request.ProviderId,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Reason = request.Reason,
            CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier)!
        };

        _context.ProviderAvailabilities.Add(availability);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Provider availability blocked successfully" });
    }

    [HttpGet("blocks")]
    public async Task<IActionResult> GetAvailabilityBlocks([FromQuery] string? providerId = null)
    {
        var query = _context.ProviderAvailabilities
            .Include(a => a.Provider)
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(providerId))
            query = query.Where(a => a.ProviderId == providerId);

        var blocks = await query.Select(a => new
        {
            id = a.Id,
            provider = new
            {
                id = a.Provider.Id,
                name = a.Provider.FullName
            },
            startDate = a.StartDate,
            endDate = a.EndDate,
            startTime = a.StartTime,
            endTime = a.EndTime,
            reason = a.Reason,
            createdAt = a.CreatedAt,
            createdBy = new
            {
                id = a.CreatedBy.Id,
                name = a.CreatedBy.FullName
            }
        }).ToListAsync();

        return Ok(blocks);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveAvailabilityBlock(int id)
    {
        var block = await _context.ProviderAvailabilities.FindAsync(id);
        if (block == null)
            return NotFound();

        _context.ProviderAvailabilities.Remove(block);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Availability block removed successfully" });
    }
}
