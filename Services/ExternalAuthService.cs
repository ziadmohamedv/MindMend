using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Mind_Mend.Models.Auth;
using Mind_Mend.Models.Users;
using System.Security.Claims;

namespace Mind_Mend.Services
{
    public class ExternalAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly TokenService _tokenService;
        private readonly GoogleAuthSettings _googleSettings;

        public ExternalAuthService(
            UserManager<User> userManager,
            TokenService tokenService,
            IOptions<GoogleAuthSettings> googleSettings)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _googleSettings = googleSettings.Value;
        }

        public async Task<(User? user, string? token, string? error)> AuthenticateGoogleUserAsync(string idToken)
        {
            try
            {
                // Validate the token with Google
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleSettings.ClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
                
                // Check if user exists
                var user = await _userManager.FindByEmailAsync(payload.Email);
                
                if (user == null)
                {
                    // Create a new user
                    user = new User
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        FirstName = payload.GivenName ?? string.Empty,
                        LastName = payload.FamilyName ?? string.Empty,
                        FullName = payload.Name ?? $"{payload.GivenName} {payload.FamilyName}",
                        Gender = "Unspecified", // Default value as Google doesn't provide gender
                        BirthDate = DateOnly.FromDateTime(DateTime.Today), // Default value as Google doesn't provide birth date
                        EmailConfirmed = payload.EmailVerified // Google already verified the email
                    };

                    var result = await _userManager.CreateAsync(user);
                    
                    if (!result.Succeeded)
                    {
                        return (null, null, "Failed to create user from Google authentication");
                    }

                    // Add external login
                    await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
                    
                    // Assign default role (Patient)
                    await _userManager.AddToRoleAsync(user, Roles.Patient);
                }
                else
                {
                    // Check if this Google account is already linked to the user
                    var logins = await _userManager.GetLoginsAsync(user);
                    var googleLogin = logins.FirstOrDefault(l => l.LoginProvider == "Google" && l.ProviderKey == payload.Subject);
                    
                    if (googleLogin == null)
                    {
                        // Link the Google account to the existing user
                        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
                    }
                }

                // Generate JWT token
                var token = await _tokenService.GenerateJwtToken(user);
                
                return (user, token, null);
            }
            catch (InvalidJwtException ex)
            {
                return (null, null, $"Invalid Google token: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (null, null, $"Error authenticating with Google: {ex.Message}");
            }
        }
    }
}