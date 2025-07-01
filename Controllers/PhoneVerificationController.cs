using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Models.Users;
using Mind_Mend.Services;

namespace Mind_Mend.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class PhoneVerificationController : ControllerBase
{
    private readonly IOtpService _otpService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<PhoneVerificationController> _logger;

    public PhoneVerificationController(
        IOtpService otpService,
        UserManager<User> userManager,
        ILogger<PhoneVerificationController> logger)
    {
        _otpService = otpService;
        _userManager = userManager;
        _logger = logger;
    }
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] PhoneVerificationRequest request)
    {
        // For testing - use a default user ID or create a test implementation
        // In production, this should get userId from JWT token
        var userId = "2c14c741-fa04-4d8a-8e68-2d5fbf93c19b"; // Using the user ID from your JWT

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Check if phone number is already verified
        if (user.PhoneNumberConfirmed)
        {
            return BadRequest("Phone number is already verified");
        }

        // Check if phone number is already in use by another user
        var existingUser = await _userManager.Users.FirstOrDefaultAsync(u =>
            u.PhoneNumber == request.PhoneNumber &&
            u.PhoneNumberConfirmed &&
            u.Id != userId);

        if (existingUser != null)
        {
            return BadRequest("This phone number is already registered to another user");
        }

        // Generate and save OTP
        var otp = _otpService.GenerateOtp();
        var success = await _otpService.SaveOtp(request.PhoneNumber, otp);

        if (!success)
            return StatusCode(500, "Failed to send OTP");

        // Save the phone number temporarily (will be confirmed after OTP verification)
        user.PhoneNumber = request.PhoneNumber;
        user.PhoneNumberConfirmed = false;
        await _userManager.UpdateAsync(user);

        return Ok(new
        {
            message = "OTP sent successfully",
            phoneNumber = request.PhoneNumber,
            otp = otp // For testing purposes - remove in production
        });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        // Verify that the phone number matches what was sent in the OTP
        if (user.PhoneNumber != request.PhoneNumber)
        {
            return BadRequest("Phone number mismatch. Please request a new OTP.");
        }

        // Verify OTP
        var isValid = await _otpService.VerifyOtp(request.PhoneNumber, request.Otp);
        if (!isValid)
            return BadRequest("Invalid OTP");

        // Update user's phone number confirmation status
        user.PhoneNumberConfirmed = true;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return StatusCode(500, "Failed to update phone number status");

        return Ok(new
        {
            message = "Phone number verified successfully",
            phoneNumber = user.PhoneNumber,
            isVerified = true
        });
    }
}