using Microsoft.AspNetCore.Mvc;
using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;

namespace Pricing.Api.Controllers;

[ApiController]
[Route("rules")]
public class RulesController : ControllerBase
{
    private readonly IRuleService _service;

    public RulesController(IRuleService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_service.GetAll());
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        var rule = _service.GetById(id);
        if (rule == null) return NotFound();

        return Ok(rule);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateRuleRequest dto)
    {
        var id = _service.Create(dto);
        return Ok(new { id });
    }

    [HttpPatch("{id}")]
    public IActionResult Update(string id, [FromBody] UpdateRuleRequest dto)
    {
        var updated = _service.Update(id, dto);
        if (!updated) return NotFound();

        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        var deleted = _service.Delete(id);
        if (!deleted) return NotFound();

        return Ok();
    }
}