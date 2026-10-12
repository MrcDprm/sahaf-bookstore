namespace Sahaf.Utility;

public class StripeSettings
{
    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
