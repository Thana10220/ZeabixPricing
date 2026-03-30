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
    if (stream == null || !stream.CanRead)
    {
        _logger.LogError("Invalid stream provided to CsvParser");
        throw new ArgumentException("Invalid file stream");
    }

    var requests = new List<QuoteRequest>();

    int lineNumber = 0;
    int successCount = 0;
    int failCount = 0;

    _logger.LogInformation("CSV parsing started");

    try
    {
        using var reader = new StreamReader(stream);

        bool isHeader = true;

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

            try
            {
                var parts = line.Split(',');

                if (parts.Length < 3)
                {
                    failCount++;
                    _logger.LogWarning("Invalid column count at line {Line}", lineNumber);
                    continue;
                }
                
                if (!decimal.TryParse(parts[0], out var weight))
                {
                    failCount++;
                    _logger.LogWarning("Invalid weight at line {Line}: {Value}", lineNumber, parts[0]);
                    continue;
                }

                var area = parts[1]?.Trim();

                if (string.IsNullOrWhiteSpace(area))
                {
                    failCount++;
                    _logger.LogWarning("Empty area at line {Line}", lineNumber);
                    continue;
                }

                if (!DateTime.TryParse(parts[2], out var requestTime))
                {
                    failCount++;
                    _logger.LogWarning("Invalid datetime at line {Line}: {Value}", lineNumber, parts[2]);
                    continue;
                }

                var request = new QuoteRequest
                {
                    Weight = weight,
                    Area = area,
                    RequestTime = requestTime.ToUniversalTime()
                };

                requests.Add(request);
                successCount++;
            }
            catch (Exception ex)
            {
                failCount++;

                _logger.LogError(ex,
                    "Unexpected error parsing line {Line}: {Content}",
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
    catch (Exception ex)
    {
        _logger.LogError(ex, "Critical error while parsing CSV");

        throw new Exception("Failed to process CSV file");
    }
}
}