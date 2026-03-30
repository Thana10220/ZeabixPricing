namespace Pricing.Application.DTOs;

public class QuoteRequest
{
    public decimal Weight { get; set; }
    public string Area { get; set; } = default!;
    public DateTime RequestTime { get; set; }
}