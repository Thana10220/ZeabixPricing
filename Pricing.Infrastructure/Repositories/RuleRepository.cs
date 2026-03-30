using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Pricing.Application.Interfaces;
using Pricing.Domain.Entities;
using Pricing.Infrastructure.Configurations;
using System.Text.Json;
using Pricing.Infrastructure.Models;

namespace Pricing.Infrastructure.Repositories;

public class RuleRepository : IRuleRepository
{
    private readonly string _filePath;
    private readonly ILogger<RuleRepository> _logger;

    public RuleRepository(
        IOptions<RuleSettings> settings,
        ILogger<RuleRepository> logger)
    {
        _filePath = settings.Value.FilePath;
        _logger = logger;
    }

    public List<Rule> GetActiveRules()
    {
        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            _filePath
        );

        _logger.LogInformation("Loading rules from {FilePath}", fullPath);

        if (!File.Exists(fullPath))
        {
            _logger.LogError("Rule file not found: {FilePath}", fullPath);
            throw new FileNotFoundException($"Rule file not found: {fullPath}");
        }

        try
        {
            var json = File.ReadAllText(fullPath);

            _logger.LogDebug("Rule file size: {Length} characters", json.Length);

            var ruleDataList = JsonSerializer.Deserialize<List<RuleData>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            ) ?? new List<RuleData>();

            _logger.LogInformation("Loaded {Count} rules from file", ruleDataList.Count);

            var rules = ruleDataList.Select(r => new Rule
            {
                Id = Guid.NewGuid().ToString(),
                Type = r.Type,
                Priority = r.Priority,
                IsActive = r.IsActive,
                EffectiveFrom = DateTime.SpecifyKind(r.EffectiveFrom, DateTimeKind.Utc),
                EffectiveTo   = DateTime.SpecifyKind(r.EffectiveTo, DateTimeKind.Utc),
                ConfigJson = JsonSerializer.SerializeToElement(r.ConfigJson)
            }).ToList();

            _logger.LogInformation("Mapped {Count} rules to domain", rules.Count);

            return rules;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize rule file: {FilePath}", fullPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading rules");
            throw;
        }
    }
}