using System.Text.Json;

namespace Pricing.Domain.Entities;

public class Rule
{
    public string Id { get; set; }
    public string Name { get; set; }

    public string Type { get; set; } = default!;

    public int Priority { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public JsonElement ConfigJson { get; set; }
}