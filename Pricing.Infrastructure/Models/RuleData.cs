namespace Pricing.Infrastructure.Models;

public class RuleData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } = default!;
    public int Priority { get; set; }
    public bool IsActive { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }

    public object ConfigJson { get; set; } = default!;
 
}