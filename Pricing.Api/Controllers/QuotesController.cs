using Microsoft.AspNetCore.RateLimiting;
using Pricing.Application.Services;

namespace Pricing.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;

[ApiController]
[Route("quotes")]
public class QuotesController : ControllerBase
{
    private readonly ILogger<QuotesController> _logger;
    private readonly CalculateQuoteService _service;
    private readonly IBulkQuoteService _bulkService;
    private readonly ICsvParser _csvParser;

    public QuotesController(
        ILogger<QuotesController> logger,
        CalculateQuoteService service,
        ICsvParser csvParser,
        IBulkQuoteService bulkService)
    {
        _logger = logger;
        _service = service;
        _bulkService = bulkService;
        _csvParser = csvParser;
    }

    [EnableRateLimiting("fixed")]
    [HttpPost("price")]
    public IActionResult Calculate([FromBody] QuoteRequest request)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId ?? "N/A"
        }))
        {
            _logger.LogInformation("Calculate price called");

            var result = _service.Execute(request);

            _logger.LogInformation("Calculate completed");

            return Ok(result);
        }
    }

    [EnableRateLimiting("bulk")]
    [HttpPost("bulk")]
    public IActionResult CreateBulk([FromBody] List<QuoteRequest> requests)
    {
        return HandleBulk(requests, "json");
    }

    [EnableRateLimiting("bulk")]
    [HttpPost("bulk/csv")]
    public async Task<IActionResult> CreateBulkCsv(IFormFile file)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId ?? "N/A"
        }))
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("CSV upload failed: file is empty");
                return BadRequest(new { error = "File is empty" });
            }

            _logger.LogInformation("CSV upload received: {FileName}, size: {Size}",
                file.FileName, file.Length);

            var requests = await _csvParser.ParseAsync(file.OpenReadStream());

            _logger.LogInformation("CSV parsed with {Count} valid rows", requests.Count);

            if (!requests.Any())
            {
                _logger.LogWarning("CSV parsing resulted in no valid data");
                return BadRequest(new { error = "No valid data" });
            }

            return HandleBulk(requests, "csv");
        }
    }

    [EnableRateLimiting("bulk")]
    [HttpGet("jobs/{jobId}")]
    public IActionResult GetJob(string jobId)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId ?? "N/A"
        }))
        {
            _logger.LogInformation("GetJob called for JobId={JobId}", jobId);

            var result = _bulkService.GetJob(jobId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Job not found: {JobId}", jobId);
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation("Job retrieved successfully: {JobId}", jobId);

            return Ok(result.Value);
        }
    }
    
    private IActionResult HandleBulk(List<QuoteRequest> requests, string source)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId ?? "N/A"
        }))
        {
            _logger.LogInformation("CreateBulk called from {Source} with {Count} items",
                source, requests?.Count);

            var result = _bulkService.CreateJob(requests);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("CreateBulk failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Job created successfully: {JobId}", result.Value);

            return Ok(new { job_id = result.Value });
        }
    }
}