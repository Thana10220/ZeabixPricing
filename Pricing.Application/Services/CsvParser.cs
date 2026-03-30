namespace Pricing.Application.Services;

using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class CsvParser : ICsvParser
{
    private readonly ILogger<CsvParser> _logger;

    public CsvParser(ILogger<CsvParser> logger)
    {
        _logger = logger;
    }

    public async Task<List<QuoteRequest>> ParseAsync(Stream stream)
    {
        var requests = new List<QuoteRequest>();

        using var reader = new StreamReader(stream);

        bool isHeader = true;
        int lineNumber = 0;
        int successCount = 0;
        int failCount = 0;

        _logger.LogInformation("CSV parsing started");

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                _logger.LogDebug("Skipping empty line at {Line}", lineNumber);
                continue;
            }

            if (isHeader)
            {
                _logger.LogDebug("Skipping header: {Header}", line);
                isHeader = false;
                continue;
            }

            _logger.LogDebug("Processing line {Line}: {Content}", lineNumber, line);

            var parts = line.Split(',');

            if (parts.Length < 3)
            {
                failCount++;
                _logger.LogWarning("Invalid column count at line {Line}", lineNumber);
                continue;
            }

            try
            {
                var request = new QuoteRequest
                {
                    Weight = decimal.Parse(parts[0]),
                    Area = parts[1].Trim(),
                    RequestTime = DateTime.Parse(parts[2]).ToUniversalTime()
                };

                requests.Add(request);
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;

                _logger.LogWarning(ex,
                    "Failed to parse line {Line}: {Content}",
                    lineNumber,
                    line);
            }
        }

        _logger.LogInformation(
            "CSV parsing completed. Total={Total}, Success={Success}, Failed={Failed}",
            lineNumber,
            successCount,
            failCount);

        return requests;
    }
}