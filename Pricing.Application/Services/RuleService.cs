using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;
using Pricing.Domain.Entities;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Pricing.Application.Services;

public class RuleService : IRuleService
{
    private readonly string _filePath;
    private readonly ILogger<RuleService> _logger;

    public RuleService(IConfiguration config, ILogger<RuleService> logger)
    {
        _filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            config["RuleSettings:FilePath"] ?? "rules.json"
        );

        _logger = logger;
    }

    public List<RuleDto> GetAll()
    {
        _logger.LogInformation("GetAll rules");

        var rules = ReadRules();

        _logger.LogInformation("Retrieved {Count} rules", rules.Count);

        return rules.Select(MapToDto).ToList();
    }

    public RuleDto GetById(string id)
    {
        _logger.LogInformation("Get rule by id: {Id}", id);

        var rule = ReadRules().FirstOrDefault(r => r.Id == id);

        if (rule == null)
        {
            _logger.LogWarning("Rule not found: {Id}", id);
            return null;
        }

        _logger.LogInformation("Rule found: {Id} ({Name})", rule.Id, rule.Name);

        return MapToDto(rule);
    }

    public string Create(CreateRuleRequest dto)
    {
        _logger.LogInformation("Creating rule: {Name}, Type={Type}, Priority={Priority}",
            dto.Name, dto.Type, dto.Priority);

        var rules = ReadRules();

        var rule = new Rule
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Type = dto.Type,
            Priority = dto.Priority,
            IsActive = dto.IsActive,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            ConfigJson = JsonSerializer.SerializeToElement(dto.ConfigJson)
        };

        rules.Add(rule);
        WriteRules(rules);

        _logger.LogInformation("Rule created: {Id} ({Name})", rule.Id, rule.Name);

        return rule.Id;
    }

    public bool Update(string id, UpdateRuleRequest dto)
    {
        _logger.LogInformation("Updating rule: {Id}", id);

        var rules = ReadRules();

        var rule = rules.FirstOrDefault(r => r.Id == id);
        if (rule == null)
        {
            _logger.LogWarning("Update failed - rule not found: {Id}", id);
            return false;
        }

        rule.Name = dto.Name;
        rule.Type = dto.Type;
        rule.Priority = dto.Priority;
        rule.IsActive = dto.IsActive;
        rule.EffectiveFrom = dto.EffectiveFrom;
        rule.EffectiveTo = dto.EffectiveTo;
        rule.ConfigJson = JsonSerializer.SerializeToElement(dto.ConfigJson);

        WriteRules(rules);

        _logger.LogInformation("Rule updated: {Id} ({Name})", rule.Id, rule.Name);

        return true;
    }

    public bool Delete(string id)
    {
        _logger.LogInformation("Deleting rule: {Id}", id);

        var rules = ReadRules();

        var rule = rules.FirstOrDefault(r => r.Id == id);
        if (rule == null)
        {
            _logger.LogWarning("Delete failed - rule not found: {Id}", id);
            return false;
        }

        rules.Remove(rule);
        WriteRules(rules);

        _logger.LogInformation("Rule deleted: {Id} ({Name})", rule.Id, rule.Name);

        return true;
    }

    // ------------------

    private List<Rule> ReadRules()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Rule file not found: {Path}", _filePath);
                return new List<Rule>();
            }

            var json = File.ReadAllText(_filePath);

            var rules = JsonSerializer.Deserialize<List<Rule>>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Rule>();

            _logger.LogDebug("Read {Count} rules from file", rules.Count);

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading rule file");
            throw;
        }
    }

    private void WriteRules(List<Rule> rules)
    {
        try
        {
            var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);

            _logger.LogDebug("Wrote {Count} rules to file", rules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing rule file");
            throw;
        }
    }

    private static RuleDto MapToDto(Rule r)
    {
        return new RuleDto
        {
            Id = r.Id,
            Name = r.Name,
            Type = r.Type,
            Priority = r.Priority,
            IsActive = r.IsActive,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo,
            ConfigJson = r.ConfigJson
        };
    }
}