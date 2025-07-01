//   using Microsoft.AspNetCore.Mvc;
//     using Mind_Mend.DTOs;
//     using Mind_Mend.Services;
//     using System.Threading.Tasks;

//     namespace Mind_Mend.Controllers
//     {
//         [Route("api/[controller]")]
//         [ApiController]
//         public class ChatController : ControllerBase
//         {
//             private readonly IChatService _chatService;

//             public ChatController(IChatService chatService)
//             {   
//                 _chatService = chatService;
//             }

//         [HttpPost("send")]
//         public async Task<IActionResult> SendMessage([FromHeader(Name = "SenderId")] string senderId, [FromBody] SendMessageDto dto)
//         {
//             Console.WriteLine($"123Attempting to send message. SenderId={senderId}");
//             try
//             {
//                 await _chatService.SendMessageAsync(senderId, dto);
//                 return Ok(new { message = "Message sent successfully." });
//             }
//             catch (ArgumentException ex)
//             {
//                 return BadRequest(new { error = ex.Message });
//             }
//             catch (Exception ex)
//             {
//                 return StatusCode(500, new { error = "An error occurred while sending the message." });
//             }
//         }


//         [HttpGet("messages/{userId}")]
//             public async Task<IActionResult> GetMessages(string userId)
//             {
//                 try
//                 {
//                     string senderId = "1"; // Temporary: Replace with authenticated user ID in production
//                     var messages = await _chatService.GetMessagesAsync(senderId, userId);
//                     return Ok(messages);
//                 }
//                 catch (ArgumentException ex)
//                 {
//                     return BadRequest(new { error = ex.Message });
//                 }
//                 catch (Exception ex)
//                 {
//                     return StatusCode(500, new { error = "An unexpected error occurred." });
//                 }
//             }

//             [HttpGet("previews")]
//             public async Task<IActionResult> GetChatPreviews()
//             {
//                 try
//                 {
//                 string userId = "1"; // Temporary: Replace with authenticated user ID in production
//                     var previews = await _chatService.GetChatPreviewsAsync(userId);
//                     return Ok(previews);
//                 }
//                 catch (ArgumentException ex)
//                 {
//                     return BadRequest(new { error = ex.Message });
//                 }
//                 catch (Exception ex)
//                 {
//                     return StatusCode(500, new { error = "An unexpected error occurred." });
//                 }
//             }
//         }
//     }


using Microsoft.AspNetCore.Mvc;
using Mind_Mend.DTOs;
using Mind_Mend.Services;
using System.Threading.Tasks;

namespace Mind_Mend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromHeader(Name = "SenderId")] string senderId, [FromBody] SendMessageDto dto)
        {
            Console.WriteLine($"123Attempting to send message. SenderId={senderId}");
            try
            {
                await _chatService.SendMessageAsync(senderId, dto);
                return Ok(new { message = "Message sent successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while sending the message." });
            }
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages([FromQuery] string user1Id, [FromQuery] string user2Id)
        {
            try
            {
                var messages = await _chatService.GetMessagesAsync(user1Id, user2Id);
                return Ok(messages);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("previews")]
        public async Task<IActionResult> GetChatPreviews([FromQuery] string userId)
        {
            Console.WriteLine($"GetChatPreviews called with userId={userId}");
            try
            {
                var previews = await _chatService.GetChatPreviewsAsync(userId);
                return Ok(previews);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }
    }
}