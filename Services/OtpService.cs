using Microsoft.Extensions.Caching.Memory;

namespace Mind_Mend.Services;

public interface IOtpService
{
    string GenerateOtp();
    Task<bool> SaveOtp(string phoneNumber, string otp);
    Task<bool> VerifyOtp(string phoneNumber, string otp);
}

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IMemoryCache cache,
        IWhatsAppService whatsAppService,
        ILogger<OtpService> logger)
    {
        _cache = cache;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public string GenerateOtp()
    {
        // Generate a 6-digit OTP
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    public async Task<bool> SaveOtp(string phoneNumber, string otp)
    {
        try
        {
            // Save OTP to cache with 5 minutes expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(GetCacheKey(phoneNumber), otp, cacheOptions);

            // Send OTP via WhatsApp
            return await _whatsAppService.SendOtp(phoneNumber, otp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving OTP");
            return false;
        }
    }

    public Task<bool> VerifyOtp(string phoneNumber, string otp)
    {
        try
        {
            var cacheKey = GetCacheKey(phoneNumber);
            if (_cache.TryGetValue(cacheKey, out string? savedOtp))
            {
                var isValid = savedOtp == otp;
                if (isValid)
                {
                    // Remove OTP from cache after successful verification
                    _cache.Remove(cacheKey);
                }
                return Task.FromResult(isValid);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP");
            return Task.FromResult(false);
        }
    }

    private string GetCacheKey(string phoneNumber) => $"otp_{phoneNumber}";
} 