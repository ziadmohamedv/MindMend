using Microsoft.AspNetCore.Mvc;
using Mind_Mend.Services;
using Mind_Mend.DTOs;

namespace Mind_Mend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallController : ControllerBase
    {
        private readonly CallService _callService;

        // ✅ Injecting the CallService through constructor
        public CallController(CallService callService)
        {
            _callService = callService;
        }

        /// <summary>
        /// Create a new call session between caller and receiver.
        /// </summary>
        [HttpPost("create")]
        public IActionResult CreateRoom([FromBody] CreateCallRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.CallerId) || string.IsNullOrEmpty(request.ReceiverId))
            {
                return BadRequest(new { message = "Invalid caller or receiver ID." });
            }

            var roomId = _callService.CreateCall(request.CallerId, request.ReceiverId);
            return Ok(new { roomId });
        }

        /// <summary>
        /// Get all currently active calls.
        /// </summary>
        [HttpGet("active")]
        public IActionResult GetActiveRooms()
        {
            var rooms = _callService.GetActiveCalls();
            return Ok(rooms);
        }

        /// <summary>
        /// End an existing call session.
        /// </summary>
        [HttpPost("end")]
        public IActionResult EndRoom([FromBody] EndCallRequest request)
        {
            if (string.IsNullOrEmpty(request.RoomId))
            {
                return BadRequest(new { message = "Room ID is required." });
            }

            var result = _callService.EndCall(request.RoomId);
            if (!result)
            {
                return NotFound(new { message = "Room not found." });
            }

            return Ok(new { message = "Call ended." });
        }
    }
}
