namespace Mind_Mend.Models.Payments;

public class PaymobSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string IntegrationId { get; set; } = string.Empty;
    public string IframeId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://accept.paymob.com/api";
    public string Currency { get; set; } = "EGP";
}
