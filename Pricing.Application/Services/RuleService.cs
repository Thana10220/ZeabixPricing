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
        try
        {
            _logger.LogInformation("GetAll rules");

            var rules = ReadRulesSafe();

            _logger.LogInformation("Retrieved {Count} rules", rules.Count);

            return rules.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll failed");
            return new List<RuleDto>();
        }
    }

    public RuleDto GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("GetById failed: id is empty");
            return null;
        }

        try
        {
            var rule = ReadRulesSafe().FirstOrDefault(r => r.Id == id);

            if (rule == null)
            {
                _logger.LogWarning("Rule not found: {Id}", id);
                return null;
            }

            return MapToDto(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetById failed: {Id}", id);
            return null;
        }
    }

    public string Create(CreateRuleRequest dto)
    {
        try
        {
            Validate(dto);

            var rules = ReadRulesSafe();

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

            WriteRulesSafe(rules);

            _logger.LogInformation("Rule created: {Id}", rule.Id);

            return rule.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create rule failed");
            throw;
        }
    }

    public bool Update(string id, UpdateRuleRequest dto)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        try
        {
            Validate(dto);

            var rules = ReadRulesSafe();

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

            WriteRulesSafe(rules);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed: {Id}", id);
            return false;
        }
    }

    public bool Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        try
        {
            var rules = ReadRulesSafe();

            var rule = rules.FirstOrDefault(r => r.Id == id);
            if (rule == null)
            {
                _logger.LogWarning("Delete failed - rule not found: {Id}", id);
                return false;
            }

            rules.Remove(rule);

            WriteRulesSafe(rules);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed: {Id}", id);
            return false;
        }
    }
    
    private List<Rule> ReadRulesSafe()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Rule file not found: {Path}", _filePath);
                return new List<Rule>();
            }

            var json = File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize<List<Rule>>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Rule>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in rules file");
            
            return new List<Rule>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error reading rules file");
            throw;
        }
    }

    private void WriteRulesSafe(List<Rule> rules)
    {
        try
        {
            var tempFile = _filePath + ".tmp";

            var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(tempFile, json);

            File.Copy(tempFile, _filePath, true);
            File.Delete(tempFile);

            _logger.LogDebug("Rules written safely: {Count}", rules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error writing rules file");
            throw;
        }
    }

    private void Validate(CreateRuleRequest dto)
    {
        if (dto == null)
            throw new ArgumentException("Request is null");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(dto.Type))
            throw new ArgumentException("Type is required");
    }

    private void Validate(UpdateRuleRequest dto)
    {
        if (dto == null)
            throw new ArgumentException("Request is null");
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