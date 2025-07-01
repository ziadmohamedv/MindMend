using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Mind_Mend.Services;

public class WhatsAppSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string VerificationTemplate { get; set; } = string.Empty;
}

public interface IWhatsAppService
{
    Task<bool> SendOtp(string phoneNumber, string otp);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://graph.facebook.com/v17.0/")
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
        
        // Log the template name being used
        _logger.LogInformation($"WhatsApp template name configured: {_settings.VerificationTemplate}");
    }

    public async Task<bool> SendOtp(string phoneNumber, string otp)
    {
        try
        {
            // Format phone number to international format (remove leading 0)
            var formattedPhone = "+20" + phoneNumber.TrimStart('0');
            
            _logger.LogInformation($"Attempting to send OTP to {formattedPhone} using template: {_settings.VerificationTemplate}");

            var message = new
            {
                messaging_product = "whatsapp",
                to = formattedPhone,
                type = "template",
                template = new
                {
                    name = _settings.VerificationTemplate,
                    language = new { code = "en_US" },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[]
                            {
                                new { type = "text", text = otp }
                            }
                        },
                        new
                        {
                            type = "button",
                            sub_type = "url",
                            index = 0,
                            parameters = new object[]
                            {
                                new { type = "text", text = "Verify" }
                            }
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(message),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_settings.PhoneNumberId}/messages",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send WhatsApp message: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp message");
            return false;
        }
    }
} 