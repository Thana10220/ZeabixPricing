using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Pricing.Application.Interfaces;
using Pricing.Domain.Entities;
using Pricing.Infrastructure.Configurations;
using Pricing.Infrastructure.Models;

namespace Pricing.Infrastructure.Repositories;

public class RuleRepository : IRuleRepository
{
    private readonly AsyncCircuitBreakerPolicy<List<Rule>> _circuitBreaker;
    private readonly string _filePath;
    private readonly ILogger<RuleRepository> _logger;

    private readonly AsyncRetryPolicy<List<Rule>> _retryPolicy;

    public RuleRepository(
        IOptions<RuleSettings> settings,
        ILogger<RuleRepository> logger)
    {
        _filePath = settings.Value.FilePath;
        _logger = logger;

        _retryPolicy = Policy<List<Rule>>
            .Handle<IOException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(200 * attempt),
                (ex, delay, retryCount, ctx) =>
                {
                    _logger.LogWarning(
                        "Retry {Retry} after {Delay}ms",
                        retryCount,
                        delay.TotalMilliseconds);
                });
        
        _circuitBreaker = Policy<List<Rule>>
            .Handle<IOException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                3,
                TimeSpan.FromSeconds(10),
                (ex, ts) =>
                {
                    _logger.LogError(
                        "Circuit breaker opened for {Time}s",
                        ts.TotalSeconds);
                },
                () => { _logger.LogInformation("Circuit breaker reset"); });
    }

    public List<Rule> GetActiveRules()
    {
        try
        {
            var policy = Policy.WrapAsync(_retryPolicy, _circuitBreaker);

            return policy.ExecuteAsync(ReadRulesInternalAsync)
                .GetAwaiter()
                .GetResult();
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit is open - fallback to empty rules");
            return new List<Rule>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetActiveRules");
            return new List<Rule>();
        }
    }

    private async Task<List<Rule>> ReadRulesInternalAsync()
    {
        var fullPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            _filePath
        );

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Rule file not found: {Path}", fullPath);
            return new List<Rule>();
        }

        string json;

        try
        {
            json = await File.ReadAllTextAsync(fullPath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error reading rule file");
            throw;
        }

        List<RuleData> ruleDataList;

        try
        {
            ruleDataList = JsonSerializer.Deserialize<List<RuleData>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            ) ?? new List<RuleData>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in rule file");

            return new List<Rule>();
        }

        try
        {
            return ruleDataList
                .Where(r => r.IsActive)
                .Where(r =>
                    DateTime.UtcNow >= r.EffectiveFrom &&
                    DateTime.UtcNow <= r.EffectiveTo)
                .Select(r => new Rule
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    Priority = r.Priority,
                    IsActive = r.IsActive,
                    EffectiveFrom = DateTime.SpecifyKind(r.EffectiveFrom, DateTimeKind.Utc),
                    EffectiveTo = DateTime.SpecifyKind(r.EffectiveTo, DateTimeKind.Utc),
                    ConfigJson = JsonSerializer.SerializeToElement(r.ConfigJson)
                })
                .OrderBy(r => r.Priority)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping rule data");
            return new List<Rule>();
        }
    }
}