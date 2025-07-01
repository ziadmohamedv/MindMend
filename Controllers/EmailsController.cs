using Microsoft.AspNetCore.Mvc;
using Mind_Mend.Services;

namespace Mind_Mend.Controllers
{
    [Route("api/emails")]
    [ApiController]
    public class EmailsController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailsController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(string receptor, string subject, string body)
        {
            await _emailService.SendEmail(receptor, subject, body);
            return Ok();
        }
    }
}
