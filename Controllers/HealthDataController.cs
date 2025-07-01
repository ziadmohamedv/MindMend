using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mind_Mend.Services;
using Mind_Mend.Models;
using Microsoft.Extensions.Logging;

namespace Mind_Mend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HealthDataController : ControllerBase
    {
        private readonly IGoogleHealthConnectService _healthConnectService;
        private readonly ILogger<HealthDataController> _logger;

        public HealthDataController(
            IGoogleHealthConnectService healthConnectService,
            ILogger<HealthDataController> logger)
        {
            _healthConnectService = healthConnectService;
            _logger = logger;
        }

        [HttpGet("latest")]
        public async Task<ActionResult<HealthData>> GetLatestHealthData()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var healthData = await _healthConnectService.GetLatestHealthDataAsync(userId);
                return Ok(healthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch health data for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "Failed to fetch health data", error = ex.Message });
            }
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncHealthData()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _healthConnectService.SyncHealthDataAsync(userId);
                if (success)
                {
                    return Ok(new { message = "Health data synchronized successfully" });
                }
                return BadRequest(new { message = "Failed to synchronize health data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to synchronize health data for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "Failed to synchronize health data", error = ex.Message });
            }
        }
    }
} 