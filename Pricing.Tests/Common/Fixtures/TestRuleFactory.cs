using Pricing.Domain.Entities;
using System.Text.Json;

namespace Pricing.Tests.Common.Fixtures;

public static class TestRuleFactory
{
    public static List<Rule> CreateRemoteAreaRule()
    {
        return new List<Rule>
        {
            new Rule
            {
                Id = Guid.NewGuid().ToString(),
                Type = "RemoteAreaSurcharge",
                Priority = 1,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                EffectiveTo = DateTime.UtcNow.AddDays(1),
                ConfigJson = JsonSerializer.SerializeToElement(new
                {
                    Areas = new[] { "เชียงใหม่" },
                    Price = 50
                })
            }
        };
    }

    public static List<Rule> CreateWeightTierRule()
    {
        return new List<Rule>
        {
            new Rule
            {
                Id = Guid.NewGuid().ToString(),
                Type = "WeightTier",
                Priority = 1,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                EffectiveTo = DateTime.UtcNow.AddDays(1),
                ConfigJson = JsonSerializer.SerializeToElement(new
                {
                    Min = 0,
                    Max = 20,
                    Price = 20
                })
            }
        };
    }

    public static List<Rule> CreateTimeWindowRule()
    {
        return new List<Rule>
        {
            new Rule
            {
                Id = Guid.NewGuid().ToString(),
                Type = "TimeWindowPromotion",
                Priority = 1,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                EffectiveTo = DateTime.UtcNow.AddDays(1),
                ConfigJson = JsonSerializer.SerializeToElement(new
                {
                    StartHour = 9,
                    EndHour = 18,
                    Discount = 10
                })
            }
        };
    }
}