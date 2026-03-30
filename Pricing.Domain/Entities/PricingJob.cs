namespace Pricing.Domain.Entities;

using Pricing.Domain.Enums;

public class PricingJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public List<PricingJobItem> Items { get; set; } = new();
    
    public int RetryCount { get; set; } = 0;   
    public int MaxRetries { get; set; }

    public string? ErrorMessage { get; set; }    

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
    
    public string CorrelationId { get; set; }
}