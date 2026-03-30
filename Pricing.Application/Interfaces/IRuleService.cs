namespace Pricing.Application.Interfaces;

using Pricing.Application.DTOs;

public interface IRuleService
{
    List<RuleDto> GetAll();
    RuleDto GetById(string id);
    string Create(CreateRuleRequest dto);
    bool Update(string id, UpdateRuleRequest dto);
    bool Delete(string id);
}