using Pricing.Application.Common;
using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;
using Pricing.Domain.Entities;
using Pricing.Domain.Enums;
using Microsoft.Extensions.Logging;

public class BulkQuoteService : IBulkQuoteService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobQueue _jobQueue;
    private readonly ILogger<BulkQuoteService> _logger;

    public BulkQuoteService(
        IJobRepository jobRepository,
        IJobQueue jobQueue,
        ILogger<BulkQuoteService> logger)
    {
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    public Result<string> CreateJob(List<QuoteRequest> requests)
    {
        _logger.LogInformation("CreateJob called");

        try
        {
            if (requests == null || !requests.Any())
            {
                _logger.LogWarning("CreateJob failed: Request is empty");
                return Result<string>.Failure("Request is empty");
            }

            _logger.LogInformation("Received {Count} requests", requests.Count);

            var valid = requests
                .Where(r => r != null)
                .ToList();

            if (!valid.Any())
            {
                _logger.LogWarning("CreateJob failed: All items are null");
                return Result<string>.Failure("All items are null");
            }

            _logger.LogInformation("Valid requests count: {ValidCount}", valid.Count);

            // 🔥 validate data level
            var invalidItems = valid
                .Where(r => r.Weight <= 0 || string.IsNullOrWhiteSpace(r.Area))
                .ToList();

            if (invalidItems.Any())
            {
                _logger.LogWarning("Invalid request items detected: {Count}", invalidItems.Count);
                return Result<string>.Failure("Some request items are invalid");
            }

            var job = new PricingJob
            {
                Items = valid.Select(r => new PricingJobItem
                {
                    Weight = r.Weight,
                    Area = r.Area,
                    RequestTime = r.RequestTime
                }).ToList()
            };

            _jobRepository.Create(job);

            _logger.LogInformation("Job created with JobId={JobId}", job.JobId);

            _jobQueue.Enqueue(job.JobId);

            _logger.LogInformation("Job {JobId} enqueued", job.JobId);

            return Result<string>.Success(job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while creating job. RequestCount={Count}",
                requests?.Count);

            return Result<string>.Failure("Internal server error");
        }
    }

    public Result<JobResponseDto> GetJob(string jobId)
    {
        _logger.LogInformation("GetJob called for JobId={JobId}", jobId);

        try
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                _logger.LogWarning("GetJob failed: jobId is empty");
                return Result<JobResponseDto>.Failure("Invalid jobId");
            }

            var job = _jobRepository.Get(jobId);

            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", jobId);
                return Result<JobResponseDto>.Failure("Job not found");
            }

            _logger.LogInformation(
                "Job found: {JobId}, Status={Status}, Retry={RetryCount}",
                job.JobId, job.Status, job.RetryCount);

            var dto = new JobResponseDto
            {
                JobId = job.JobId,
                Status = job.Status,
                RetryCount = job.RetryCount,
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt,
                Results = job.Status == JobStatus.Completed
                    ? job.Items.Select(i => new JobItemDto
                    {
                        Weight = i.Weight,
                        Area = i.Area,
                        FinalPrice = i.FinalPrice,
                        AppliedRules = i.AppliedRules
                    }).ToList()
                    : null
            };

            _logger.LogDebug("Returning job result for JobId={JobId}", jobId);

            return Result<JobResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while retrieving job {JobId}", jobId);

            return Result<JobResponseDto>.Failure("Internal server error");
        }
    }
}