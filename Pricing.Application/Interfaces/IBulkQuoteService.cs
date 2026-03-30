using Pricing.Application.Common;
using Pricing.Application.DTOs;

public interface IBulkQuoteService
{
    Result<string> CreateJob(List<QuoteRequest> requests);
    Result<JobResponseDto> GetJob(string jobId);
}