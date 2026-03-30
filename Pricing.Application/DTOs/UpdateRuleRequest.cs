namespace Pricing.Application.DTOs;

public class UpdateRuleRequest
{
    public string Name { get; set; }
    public string Type { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public object ConfigJson { get; set; }
}