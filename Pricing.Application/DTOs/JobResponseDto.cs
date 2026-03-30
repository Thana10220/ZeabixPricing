namespace Pricing.Application.DTOs;

using Pricing.Domain.Enums;

public class JobResponseDto
{
    public string JobId { get; set; }
    public JobStatus Status { get; set; }

    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public List<JobItemDto>? Results { get; set; }
}

public class JobItemDto
{
    public decimal Weight { get; set; }
    public string Area { get; set; }

    public decimal? FinalPrice { get; set; }
    public List<string> AppliedRules { get; set; }
}