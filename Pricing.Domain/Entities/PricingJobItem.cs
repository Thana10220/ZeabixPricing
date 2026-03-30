namespace Pricing.Domain.Entities;

public class PricingJobItem
{
    public decimal Weight { get; set; }
    public string Area { get; set; }
    public DateTime RequestTime { get; set; }
    public decimal? FinalPrice { get; set; }
    public List<string> AppliedRules { get; set; } = new();
}