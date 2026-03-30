namespace Pricing.Application.Interfaces;

using Pricing.Domain.Entities;

public interface IRuleRepository
{
    List<Rule> GetActiveRules();
}