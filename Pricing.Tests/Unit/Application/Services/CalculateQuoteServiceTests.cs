using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;
using Pricing.Domain.Entities;
using Pricing.Tests.Common.Fixtures;

namespace Pricing.Tests.Unit.Application.Services;

public class CalculateQuoteServiceTests
{
    [Fact]
    public void Should_Apply_RemoteArea()
    {
        var mockRepo = new Mock<IRuleRepository>();

        mockRepo.Setup(x => x.GetActiveRules())
            .Returns(TestRuleFactory.CreateRemoteAreaRule());

        var service = new CalculateQuoteService(
            mockRepo.Object,
            NullLogger<CalculateQuoteService>.Instance);

        var request = new QuoteRequest
        {
            Weight = 10,
            Area = "เชียงใหม่",
            RequestTime = DateTime.UtcNow
        };

        var result = service.Execute(request);

        Assert.True(result.FinalPrice > 100);
    }
    
    [Fact]
    public void Should_Apply_Correct_WeightTier_On_Boundary()
    {
      
        var mockRepo = new Mock<IRuleRepository>();

        var rules = new List<Rule>
        {
            new Rule
            {
                Type = "WeightTier",
                Priority = 1,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                EffectiveTo = DateTime.UtcNow.AddDays(1),
                ConfigJson = JsonSerializer.SerializeToElement(new
                {
                    Min = 0,
                    Max = 10,
                    Price = 0
                })
            },
            new Rule
            {
                Type = "WeightTier",
                Priority = 2,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                EffectiveTo = DateTime.UtcNow.AddDays(1),
                ConfigJson = JsonSerializer.SerializeToElement(new
                {
                    Min = 10,
                    Max = 30,
                    Price = 20
                })
            }
        };

        mockRepo.Setup(x => x.GetActiveRules()).Returns(rules);

        var service = new CalculateQuoteService(
            mockRepo.Object,
            NullLogger<CalculateQuoteService>.Instance);

        var request = new QuoteRequest
        {
            Weight = 10, 
            Area = "กรุงเทพ",
            RequestTime = DateTime.UtcNow
        };
        
        var result = service.Execute(request);
        
        Assert.Equal(100, result.FinalPrice); 
        Assert.Single(result.AppliedRules);   
    }
    
    [Fact]
    public void Should_Apply_Only_First_Matching_Rule_Per_Type()
    {
        var mockRepo = new Mock<IRuleRepository>();

        var rules = new List<Rule>
        {
            new Rule
            {
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
            },
            new Rule
            {
                Type = "RemoteAreaSurcharge",
                Priority = 2,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                EffectiveTo = DateTime.UtcNow.AddDays(1),
                ConfigJson = JsonSerializer.SerializeToElement(new
                {
                    Areas = new[] { "เชียงใหม่" },
                    Price = 100
                })
            }
        };

        mockRepo.Setup(x => x.GetActiveRules()).Returns(rules);

        var service = new CalculateQuoteService(
            mockRepo.Object,
            NullLogger<CalculateQuoteService>.Instance);

        var request = new QuoteRequest
        {
            Weight = 5,
            Area = "เชียงใหม่",
            RequestTime = DateTime.UtcNow
        };
        
        var result = service.Execute(request);

        Assert.Equal(150, result.FinalPrice); 
        Assert.Single(result.AppliedRules);  
    }
}