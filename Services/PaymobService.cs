using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Mind_Mend.Models.Payments;

namespace Mind_Mend.Services;

public interface IPaymentService
{
    Task<string> CreatePaymentToken(decimal amount, string orderId, string customerEmail, string customerPhone);
    Task<bool> VerifyPayment(string transactionId);
} 

public class PaymobService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly PaymobSettings _paymobSettings;
    private readonly ILogger<PaymobService> _logger;

    public PaymobService(
        IOptions<PaymobSettings> paymobSettings,
        ILogger<PaymobService> logger)
    {
        _paymobSettings = paymobSettings.Value;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Log configuration values for debugging
        _logger.LogInformation($"PaymobSettings - ApiKey: {_paymobSettings.ApiKey?.Length ?? 0} chars");
        _logger.LogInformation($"PaymobSettings - IntegrationId: {_paymobSettings.IntegrationId}");
        _logger.LogInformation($"PaymobSettings - IframeId: {_paymobSettings.IframeId}");
        _logger.LogInformation($"PaymobSettings - BaseUrl: {_paymobSettings.BaseUrl}");
    }

    public async Task<string> CreatePaymentToken(decimal amount, string orderId, string customerEmail, string customerPhone)
    {
        try
        {
            _logger.LogInformation($"Creating payment token for order {orderId}, amount: {amount}");
            
            // Step 1: Authentication request
            var authToken = await GetAuthToken();
            if (string.IsNullOrEmpty(authToken))
            {
                _logger.LogError("Failed to get authentication token from Paymob");
                return string.Empty;
            }
            
            // Step 2: Order registration
            var paymobOrderId = await RegisterOrder(authToken, amount, orderId);
            if (string.IsNullOrEmpty(paymobOrderId))
            {
                _logger.LogError("Failed to register order with Paymob");
                return string.Empty;
            }
            
            // Step 3: Payment key request
            var paymentKey = await GetPaymentKey(authToken, amount, paymobOrderId, customerEmail, customerPhone);
            if (string.IsNullOrEmpty(paymentKey))
            {
                _logger.LogError("Failed to get payment key from Paymob");
                return string.Empty;
            }
            
            return $"https://accept.paymob.com/api/acceptance/iframes/{_paymobSettings.IframeId}?payment_token={paymentKey}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment token");
            return string.Empty;
        }
    }

    public async Task<bool> VerifyPayment(string transactionId)
    {
        try
        {
            _logger.LogInformation($"Verifying payment for transaction {transactionId}");
            
            // Get auth token
            var authToken = await GetAuthToken();
            if (string.IsNullOrEmpty(authToken))
            {
                _logger.LogError("Failed to get authentication token from Paymob");
                return false;
            }
            
            // Query transaction status
            var url = $"{_paymobSettings.BaseUrl}/acceptance/transactions/{transactionId}";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to verify payment: {response.StatusCode}");
                return false;
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var transaction = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Check if transaction is successful
            if (transaction.TryGetProperty("success", out var success) && success.GetBoolean())
            {
                if (transaction.TryGetProperty("is_refunded", out var isRefunded) && !isRefunded.GetBoolean())
                {
                    if (transaction.TryGetProperty("is_void", out var isVoid) && !isVoid.GetBoolean())
                    {
                        _logger.LogInformation($"Payment verified successfully for transaction {transactionId}");
                        return true;
                    }
                }
            }
            
            _logger.LogWarning($"Payment verification failed for transaction {transactionId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error verifying payment for transaction {transactionId}");
            return false;
        }
    }

    private async Task<string> GetAuthToken()
    {
        try
        {
            var url = $"{_paymobSettings.BaseUrl}/auth/tokens";
            var payload = new
            {
                api_key = _paymobSettings.ApiKey
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get auth token: {response.StatusCode}");
                return string.Empty;
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (responseJson.TryGetProperty("token", out var token))
            {
                return token.GetString() ?? string.Empty;
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auth token");
            return string.Empty;
        }
    }

    private async Task<string> RegisterOrder(string authToken, decimal amount, string orderId)
    {
        try
        {
            var orderUrl = $"{_paymobSettings.BaseUrl}/ecommerce/orders";
            
            // Generate a unique merchant order ID by combining the original ID with a timestamp
            var uniqueOrderId = orderId;
            _logger.LogInformation($"Generated unique order ID: {uniqueOrderId}");

            var orderPayload = new
            {
                auth_token = authToken,
                delivery_needed = false,
                amount_cents = (int)(amount * 100),
                currency = _paymobSettings.Currency,
                merchant_order_id = uniqueOrderId,
                items = Array.Empty<object>()
            };
            
            var orderContent = new StringContent(
                JsonSerializer.Serialize(orderPayload),
                Encoding.UTF8,
                "application/json");
            
            var orderResponse = await _httpClient.PostAsync(orderUrl, orderContent);
            if (!orderResponse.IsSuccessStatusCode)
            {
                var errorContent = await orderResponse.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to create order: {orderResponse.StatusCode}, Response: {errorContent}");
                return string.Empty;
            }
            
            var orderResponseContent = await orderResponse.Content.ReadAsStringAsync();
            var orderResponseJson = JsonSerializer.Deserialize<JsonElement>(orderResponseContent);
            
            if (orderResponseJson.TryGetProperty("id", out var paymobOrderId))
            {
                return paymobOrderId.ToString();
            }
            
            _logger.LogError("Could not find order ID in response");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering order");
            return string.Empty;
        }
    }

    private async Task<string> GetPaymentKey(string authToken, decimal amount, string orderId, string customerEmail, string customerPhone)
    {
        try
        {
            var url = $"{_paymobSettings.BaseUrl}/acceptance/payment_keys";
            
            // Parse integration ID
            if (!int.TryParse(_paymobSettings.IntegrationId, out var integrationId))
            {
                _logger.LogError($"Invalid integration ID format: {_paymobSettings.IntegrationId}");
                return string.Empty;
            }

            var payload = new
            {
                auth_token = authToken,
                amount_cents = (int)(amount * 100),
                expiration = 3600,
                order_id = orderId,
                billing_data = new
                {
                    apartment = "NA",
                    email = customerEmail,
                    floor = "NA",
                    first_name = "NA",
                    street = "NA",
                    building = "NA",
                    phone_number = customerPhone,
                    shipping_method = "NA",
                    postal_code = "NA",
                    city = "NA",
                    country = "EG",
                    last_name = "NA",
                    state = "NA"
                },
                currency = _paymobSettings.Currency,
                integration_id = integrationId
            };
            
            _logger.LogInformation($"Requesting payment key for order {orderId} with integration ID {integrationId}");
            
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to get payment key: {response.StatusCode}, Response: {errorContent}");
                return string.Empty;
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (responseJson.TryGetProperty("token", out var token))
            {
                return token.GetString() ?? string.Empty;
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment key");
            return string.Empty;
        }
    }
}
