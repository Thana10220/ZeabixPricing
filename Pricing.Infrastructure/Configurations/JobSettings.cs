namespace Pricing.Infrastructure.Configuration;

public class JobSettings
{
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
}