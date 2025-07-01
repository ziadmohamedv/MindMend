//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Mind_Mend.Data;
//using Mind_Mend.DTOs;
//using Mind_Mend.Models;
//using Mind_Mend.Models.Users;
//using Mind_Mend.Services;

//namespace Mind_Mend.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class UsersController : ControllerBase
//    {
//        private readonly IPasswordHasher<User> _passwordHasher;
//        private readonly ApplicationDbContext _context;
//        private readonly IConfiguration _configuration;
//        private readonly IEmailService _emailService;

//        public UsersController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, IEmailService emailService, IConfiguration configuration)
//        {
//            _context = context;
//            _passwordHasher = passwordHasher;
//            _emailService = emailService;
//            _configuration = configuration;
//        }

//        [HttpPost]
//        public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto userDto)
//        {
//            if (userDto.Password != userDto.ReEnterPassword)
//            {
//                return BadRequest("Passwords do not match.");
//            }

//            var existingUser = await _context.Users.SingleOrDefaultAsync(u => u.Email == userDto.Email);
//            if (existingUser != null)
//            {
//                return Conflict("Email is already registered.");
//            }

//            var parsedRole = Roles.Patient;

//                var user = new User
//                {
//                    UserName = userDto.Username,
//                    Role = parsedRole,
//                    FirstName = userDto.FirstName,
//                    LastName = userDto.LastName,
//                    FullName = $"{userDto.FirstName} {userDto.LastName}",
//                    Gender = userDto.Gender,
//                    BirthDate = userDto.BirthDate,
//                    Email = userDto.Email,  
//                    EmailConfirmed = false,
//                    EmailVerificationToken = Guid.NewGuid().ToString()
//                };

//            user.Password = _passwordHasher.HashPassword(user, userDto.Password);
//            _context.Users.Add(user);
//            await _context.SaveChangesAsync();

//            // Prepare verification link
//            var backendUrl = $"{Request.Scheme}://{Request.Host}";
//            var verificationUrl = $"{backendUrl}/api/users/verify-email?token={user.EmailVerificationToken}";
//            var emailBody = $"Please verify your email by clicking <a href='{verificationUrl}'>here</a>.";

//            await _emailService.SendEmail(user.Email, "Email Verification", emailBody);

//            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, UserToDto(user));
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

//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
//        {
//            return await _context.Users
//                .Select(user => UserToDto(user))
//                .ToListAsync();
//        }

//        [HttpGet("{id}")]
//        public async Task<ActionResult<UserDto>> GetUser(int id)
//        {
//            var user = await _context.Users.FindAsync(id);

//            if (user == null)
//            {
//                return NotFound();
//            }

//            return UserToDto(user);
//        }
//    }
//}
