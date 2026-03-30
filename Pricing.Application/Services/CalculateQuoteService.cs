using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pricing.Application.Common.Constants;
using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;

public class CalculateQuoteService
{
    private readonly ILogger<CalculateQuoteService> _logger;
    private readonly IRuleRepository _ruleRepo;

    public CalculateQuoteService(
        IRuleRepository ruleRepo,
        ILogger<CalculateQuoteService> logger)
    {
        _ruleRepo = ruleRepo;
        _logger = logger;
    }

    public QuoteResponse Execute(QuoteRequest request)
    {
        _logger.LogInformation("CalculateQuote started: Weight={Weight}, Area={Area}",
            request.Weight, request.Area);
        try
        {
            decimal price = 100;

            var rules = _ruleRepo.GetActiveRules();
            _logger.LogInformation("Loaded {Count} rules", rules.Count);

            var now = request.RequestTime == default
                ? DateTime.UtcNow
                : request.RequestTime.ToUniversalTime();

            var validRules = rules
                .Where(r => r.IsActive)
                .Where(r => r.EffectiveFrom <= now && r.EffectiveTo >= now)
                .OrderBy(r => r.Priority)
                .ToList();

            _logger.LogInformation("Valid rules after filtering: {Count}", validRules.Count);

            var appliedRules = new List<string>();

            var rulesByType = validRules.GroupBy(r => r.Type);

            foreach (var group in rulesByType)
            {
                _logger.LogDebug("Processing rule group: {Type}", group.Key);

                foreach (var rule in group)
                {
                    var matched = false;

                    switch (rule.Type)
                    {
                        case RuleTypes.WeightTier:
                        {
                            var config = JsonSerializer.Deserialize<WeightTierConfig>(
                                rule.ConfigJson.GetRawText(),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (request.Weight >= config.Min && request.Weight <= config.Max)
                            {
                                price += config.Price;

                                _logger.LogInformation(
                                    "Applied WeightTier: {Min}-{Max}, +{Price}, NewPrice={NewPrice}",
                                    config.Min, config.Max, config.Price, price);

                                appliedRules.Add($"WeightTier({config.Min}-{config.Max})");
                                matched = true;
                            }

                            break;
                        }

                        case RuleTypes.RemoteAreaSurcharge:
                        {
                            var config = JsonSerializer.Deserialize<RemoteAreaConfig>(
                                rule.ConfigJson.GetRawText(),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (config.Areas.Any(a =>
                                    a.Trim().ToLower() == request.Area.Trim().ToLower()))
                            {
                                price += config.Price;

                                _logger.LogInformation(
                                    "Applied RemoteAreaSurcharge: Area={Area}, +{Price}, NewPrice={NewPrice}",
                                    request.Area, config.Price, price);

                                appliedRules.Add("RemoteAreaSurcharge");
                                matched = true;
                            }

                            break;
                        }

                        case RuleTypes.TimeWindowPromotion:
                        {
                            var config = JsonSerializer.Deserialize<TimeWindowConfig>(
                                rule.ConfigJson.GetRawText(),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            var hour = now.Hour;

                            if (hour >= config.StartHour && hour <= config.EndHour)
                            {
                                price -= config.Discount;

                                _logger.LogInformation(
                                    "Applied TimeWindowPromotion: Hour={Hour}, -{Discount}, NewPrice={NewPrice}",
                                    hour, config.Discount, price);

                                appliedRules.Add("TimeWindowPromotion");
                                matched = true;
                            }

                            break;
                        }
                    }

                    if (matched)
                    {
                        _logger.LogDebug("Rule matched, skipping remaining rules in group {Type}", group.Key);
                        break;
                    }
                }
            }

            _logger.LogInformation("Calculation completed: FinalPrice={Price}, AppliedRules={Count}",
                price, appliedRules.Count);


            return new QuoteResponse
            {
                FinalPrice = price,
                AppliedRules = appliedRules
            };
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in CalculateQuote");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CalculateQuote");

            throw new Exception("Internal error while calculating quote");
        }
    }
}