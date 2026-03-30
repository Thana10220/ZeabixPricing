namespace Pricing.Infrastructure.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Pricing.Application.Interfaces;
using Pricing.Application.Services;
using Pricing.Application.DTOs;
using Pricing.Domain.Enums;
using Pricing.Infrastructure.Configuration;

public class JobWorker : BackgroundService
{
    private readonly IJobQueue _queue;
    private readonly IServiceProvider _sp;
    private readonly JobSettings _settings;
    private readonly ILogger<JobWorker> _logger;

    public JobWorker(
        IJobQueue queue,
        IServiceProvider sp,
        IOptions<JobSettings> options,
        ILogger<JobWorker> logger)
    {
        _queue = queue;
        _sp = sp;
        _settings = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _queue.DequeueAsync(stoppingToken);

            using var scope = _sp.CreateScope();

            var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
            var useCase = scope.ServiceProvider.GetRequiredService<CalculateQuoteService>();

            var job = jobRepo.Get(jobId);

            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", jobId);
                continue;
            }

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["JobId"] = job.JobId,
                ["CorrelationId"] = job.CorrelationId ?? "N/A"
            }))
            {
                try
                {
                    _logger.LogInformation("Processing job");

                    job.Status = JobStatus.Processing;
                    jobRepo.Update(job);

                    foreach (var item in job.Items)
                    {
                        _logger.LogDebug("Processing item Weight={Weight}, Area={Area}",
                            item.Weight, item.Area);

                        var result = useCase.Execute(new QuoteRequest
                        {
                            Weight = item.Weight,
                            Area = item.Area,
                            RequestTime = item.RequestTime
                        });

                        item.FinalPrice = result.FinalPrice;
                        item.AppliedRules = result.AppliedRules;
                    }

                    job.Status = JobStatus.Completed;
                    job.CompletedAt = DateTime.UtcNow;
                    job.ErrorMessage = null;

                    _logger.LogInformation("Job completed successfully");
                }
                catch (Exception ex)
                {
                    job.RetryCount++;
                    job.ErrorMessage = ex.Message;

                    if (job.RetryCount <= _settings.MaxRetries)
                    {
                        job.Status = JobStatus.Pending;

                        _logger.LogWarning(ex,
                            "Job failed. Retrying {RetryCount}/{MaxRetries}",
                            job.RetryCount,
                            _settings.MaxRetries);

                        var delay = TimeSpan.FromSeconds(_settings.RetryDelaySeconds);
                        await Task.Delay(delay, stoppingToken);

                        _queue.Enqueue(job.JobId);
                    }
                    else
                    {
                        job.Status = JobStatus.Failed;

                        _logger.LogError(ex,
                            "Job failed permanently after {RetryCount} retries",
                            job.RetryCount);
                    }
                }

                jobRepo.Update(job);
            }
        }

        _logger.LogInformation("JobWorker stopped");
    }
}