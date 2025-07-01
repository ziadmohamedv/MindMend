using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mind_Mend.Models.Diagnosis;
using Mind_Mend.Services;

namespace Mind_Mend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosisController : ControllerBase
{
    private readonly IDiagnosisService _diagnosisService;

    public DiagnosisController(IDiagnosisService diagnosisService)
    {
        _diagnosisService = diagnosisService;
    }

    [HttpPost("initial-assessment")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<InitialDiagnosisResponse>> GetInitialDiagnosis(
        [FromBody] InitialDiagnosisRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var diagnosis = await _diagnosisService.GetInitialDiagnosis(request);
            
            // If immediate support is required, we should log this
            if (diagnosis.RequiresImmediate24HourSupport || diagnosis.UrgencyLevel == "Emergency")
            {
                // TODO: Implement emergency notification system
                // This could involve notifying on-call staff or sending alerts
            }

            return Ok(diagnosis);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"An error occurred while processing the diagnosis: {ex.Message}" });
        }
    }
} 