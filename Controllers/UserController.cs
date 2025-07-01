using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Mind_Mend.Models.Users;
using Mind_Mend.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Mind_Mend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;
        private readonly IEmailService _emailService;

        public UserController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                FullName = $"{model.FirstName} {model.LastName}",
                Gender = model.Gender,
                BirthDate = model.BirthDate,
                EmailConfirmed = false // Email needs to be confirmed
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign default role (Patient)
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Patient);
            
            if (!roleResult.Succeeded)
            {
                // Log the error
                return StatusCode(500, new { message = "User created but role assignment failed", errors = roleResult.Errors });
            }

            try
            {
                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Encode the token for URL transmission
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                
                // Create confirmation link
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/user/confirm-email?userId={user.Id}&token={encodedToken}";
                
                // Create email message
                var emailMessage = $@"<html>
                    <body>
                        <h2>Welcome to Mind-Mend!</h2>
                        <p>Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.</p>
                        <p>If you did not create this account, you can ignore this email.</p>
                        <p>Alternatively, you can use the following token to confirm your email: {encodedToken}</p>
                    </body>
                </html>";
                
                Console.WriteLine($"Attempting to send confirmation email to {user.Email}");
                
                // Send confirmation email - our updated EmailService will throw exceptions on failure
                await _emailService.SendEmailAsync(user.Email, "Confirm your Mind-Mend account", emailMessage);
                
                Console.WriteLine($"Confirmation email sent to {user.Email}");
                
                // Generate JWT token for phone verification
                var jwtToken = await _tokenService.GenerateJwtToken(user);
                var roles = await _userManager.GetRolesAsync(user);
                
                return Ok(new { 
                    message = "User registered successfully. Please check your email to confirm your account.", 
                    userId = user.Id, 
                    role = Roles.Patient,
                    token = jwtToken,
                    email = user.Email,
                    fullName = user.FullName,
                    roles = roles
                });
            }
            catch (InvalidOperationException ex)
            {
                // This indicates a configuration error
                Console.WriteLine($"Email configuration error: {ex.Message}");
                return StatusCode(500, new { 
                    message = "User registered but email confirmation failed due to server configuration.", 
                    error = "Email service misconfigured", 
                    userId = user.Id 
                });
            }
            catch (Exception ex)
            {
                // Log the error but continue with registration
                Console.WriteLine($"Failed to send confirmation email: {ex.Message}");
                
                // For debugging purposes
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }

            return Ok(new { message = "User registered successfully. Please check your email to confirm your account.", userId = user.Id, role = Roles.Patient });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return BadRequest(new { message = "User ID and token are required" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Decode the token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            
            // Confirm the email
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            
            if (!result.Succeeded)
                return BadRequest(new { message = "Email confirmation failed", errors = result.Errors });

            return Ok(new { message = "Thank you for confirming your email. You can now log in to your account." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { 
                    message = "We couldn't find an account with this email address. Please check your email or register for a new account." 
                });

            // First verify the password before checking email confirmation
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!isPasswordValid)
                return Unauthorized(new { 
                    message = "The password you entered is incorrect. Please try again." 
                });

            // Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                // Generate new confirmation token
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmationToken));
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/user/confirm-email?userId={user.Id}&token={encodedToken}";
                
                // Send confirmation email again
                var emailMessage = $@"<html>
                    <body>
                        <h2>Welcome to Mind-Mend!</h2>
                        <p>Your account is almost ready. To start using Mind-Mend, please verify your email address.</p>
                        <p>Click the button below to verify your email:</p>
                        <p style='text-align: center;'>
                            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
                               style='background-color: #4CAF50; 
                                      color: white; 
                                      padding: 14px 20px; 
                                      text-decoration: none; 
                                      border-radius: 4px;
                                      display: inline-block;'>
                                Verify Email Address
                            </a>
                        </p>
                        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                        <p>{HtmlEncoder.Default.Encode(callbackUrl)}</p>
                        <p>This link will expire in 24 hours for security reasons.</p>
                        <p>If you didn't create a Mind-Mend account, please ignore this email.</p>
                    </body>
                </html>";

                if (string.IsNullOrEmpty(user.Email))
                {
                    return Unauthorized(new { 
                        message = "There's an issue with your email address. Please contact support." 
                    });
                }
                
                await _emailService.SendEmailAsync(user.Email, "Verify your Mind-Mend email address", emailMessage);

                return Unauthorized(new { 
                    status = "unconfirmed_email",
                    message = "Your account is not activated yet. We've sent a verification link to your email address. Please check your inbox and click the link to activate your account. If you don't see the email, please check your spam folder.",
                    email = user.Email // Send back the email address so the frontend can display it
                });
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            if (!result.Succeeded)
                return Unauthorized(new { 
                    message = "Something went wrong while signing in. Please try again." 
                });

            // Generate JWT token
            var jwtToken = await _tokenService.GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                roles = roles,
                token = jwtToken
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return BadRequest(new { message = "Role does not exist" });

            // Check if user is already in the role
            if (await _userManager.IsInRoleAsync(user, model.RoleName))
                return BadRequest(new { message = "User already has this role" });

            // Add user to role
            var result = await _userManager.AddToRoleAsync(user, model.RoleName);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Role assigned successfully" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Check if user is in the role
            if (!await _userManager.IsInRoleAsync(user, model.RoleName))
                return BadRequest(new { message = "User does not have this role" });

            // Remove user from role
            var result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { message = "Role removed successfully" });
        }

        [Authorize]
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });
                
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                firstName = user.FirstName,
                lastName = user.LastName,
                gender = user.Gender,
                birthDate = user.BirthDate,
                roles = roles
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            var userDtos = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new
                {
                    id = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    roles = roles
                });
            }

            return Ok(userDtos);
        }
    }
}
