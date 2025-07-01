using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.Models.Users;
using Mind_Mend.Services;
using System.Security.Claims;

namespace Mind_Mend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly MindMendDbContext _context;
    private readonly INotificationService _notificationService;

    public NotificationController(
        UserManager<User> userManager,
        MindMendDbContext context,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _context = context;
        _notificationService = notificationService;
    }

    [HttpPost("register-token")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrEmpty(request.FcmToken))
            return BadRequest("FCM token is required");

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.FcmToken = request.FcmToken;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "FCM token registered successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Failed to register FCM token" + ex.Message);
        }
    }

    [HttpDelete("unregister-token")]
    public async Task<IActionResult> UnregisterFcmToken()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            user.FcmToken = null;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "FCM token unregistered successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Failed to unregister FCM token" + ex.Message);
        }
    }

    [HttpGet("token-status")]
    public async Task<IActionResult> GetTokenStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                hasToken = !string.IsNullOrEmpty(user.FcmToken),
                tokenRegistered = !string.IsNullOrEmpty(user.FcmToken)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Failed to get token status" + ex.Message);
        }
    }

    [HttpPost("test-notification")]
    public async Task<IActionResult> TestNotification()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            if (string.IsNullOrEmpty(user.FcmToken))
                return BadRequest("No FCM token registered for this user");

            var success = await _notificationService.SendNotificationAsync(
                user.FcmToken,
                "Test Notification",
                "This is a test notification from Mind-Mend!",
                new Dictionary<string, string>
                {
                    { "type", "test" },
                    { "timestamp", DateTime.UtcNow.ToString("O") }
                });

            if (success)
            {
                return Ok(new { message = "Test notification sent successfully" });
            }
            else
            {
                return StatusCode(500, "Failed to send test notification");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error sending test notification - " + ex.Message);
        }
    }
}

public class RegisterFcmTokenRequest
{
    public string FcmToken { get; set; } = string.Empty;
}