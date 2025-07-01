using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mind_Mend.Data;
using Mind_Mend.DTOs;
using Mind_Mend.Models;
using Mind_Mend.Models.Users;

namespace Mind_Mend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // <-- This is CRITICAL
    public class AuthController : ControllerBase
    {
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly MindMendDbContext _context;

        public AuthController(IPasswordHasher<User> passwordHasher, MindMendDbContext context)
        {
            _passwordHasher = passwordHasher;
            _context = context;
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Verification token is required.");
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.EmailVerificationToken == token);
            if (user == null)
            {
                return BadRequest("Invalid or expired verification token.");
            }

            user.EmailConfirmed = true;
            user.EmailVerificationToken = null;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("Email verified successfully.");
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserName == loginDto.Username); // Fixed: Changed 'Username' to 'UserName'
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Invalid username or password.");
            }

            return UserToDto(user);
        }

        private static UserDto UserToDto(User user) =>
            new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Role = user.Role?.ToString(), // Fixed: Convert UserRole to string
                FullName = user.FullName,
                Email = user.Email
            };
    }
}

//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Mind_Mend.Data;
//using Mind_Mend.DTOs;
//using Mind_Mend.Models;

//namespace Mind_Mend.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class AuthController : ControllerBase
//    {
//        private readonly IPasswordHasher<User> _passwordHasher;
//        private readonly ApplicationDbContext _context;

//        public AuthController(IPasswordHasher<User> passwordHasher, ApplicationDbContext context)
//        {
//            _passwordHasher = passwordHasher;
//            _context = context;
//        }

//        [HttpGet("verify-email")]
//        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
//        {
//            if (string.IsNullOrEmpty(token))
//            {
//                return BadRequest("Verification token is required.");
//            }

//            var user = await _context.Users.SingleOrDefaultAsync(u => u.EmailVerificationToken == token);
//            if (user == null)
//            {
//                return BadRequest("Invalid or expired verification token.");
//            }

//            user.EmailConfirmed = true;
//            user.EmailVerificationToken = null;

//            _context.Users.Update(user);
//            await _context.SaveChangesAsync();

//            return Ok("Email verified successfully.");
//        }

//        [HttpPost("login")]
//        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
//        {
//            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == loginDto.Username);
//            if (user == null)
//            {
//                return Unauthorized("Invalid username or password.");
//            }

//            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password);
//            if (verificationResult == PasswordVerificationResult.Failed)
//            {
//                return Unauthorized("Invalid username or password.");
//            }

//            return UserToDto(user);
//        }

//        private static UserDto UserToDto(User user) =>
//            new UserDto
//            {
//                Id = user.Id,
//                Username = user.Username,
//                Role = user.Role,
//                FullName = user.FullName,
//                Email = user.Email
//            };
//    }
//}
