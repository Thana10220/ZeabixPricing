namespace Pricing.Application.DTOs;

public class QuoteResponse
{
    public decimal FinalPrice { get; set; }
    public List<string> AppliedRules { get; set; } = new();
}