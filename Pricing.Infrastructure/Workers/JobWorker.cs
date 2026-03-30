using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pricing.Application.DTOs;
using Pricing.Application.Interfaces;
using Pricing.Domain.Entities;
using Pricing.Domain.Enums;
using Pricing.Infrastructure.Configuration;

namespace Pricing.Infrastructure.Workers;

public class JobWorker : BackgroundService
{
    private readonly ILogger<JobWorker> _logger;
    private readonly IJobQueue _queue;
    private readonly JobSettings _settings;
    private readonly IServiceProvider _sp;

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
            string jobId;
            
            try
            {
                jobId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("JobWorker is stopping (cancellation requested)");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while dequeuing job");
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            try
            {
                using var scope = _sp.CreateScope();

                var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                var useCase = scope.ServiceProvider.GetRequiredService<CalculateQuoteService>();

                var job = jobRepo.Get(jobId);

                if (job == null)
                {
                    _logger.LogWarning("Job not found: {JobId}", jobId);
                    continue;
                }

                if (job.Status == JobStatus.Completed)
                {
                    _logger.LogWarning("Skip completed job: {JobId}", jobId);
                    continue;
                }

                _logger.LogInformation("Processing job {JobId}", jobId);

                SafeUpdate(jobRepo, job, j => j.Status = JobStatus.Processing);

                foreach (var item in job.Items)
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 🔥 timeout per item

                        var result = useCase.Execute(new QuoteRequest
                        {
                            Weight = item.Weight,
                            Area = item.Area,
                            RequestTime = item.RequestTime
                        });

                        item.FinalPrice = result.FinalPrice;
                        item.AppliedRules = result.AppliedRules;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Item failed in job {JobId}, Weight={Weight}, Area={Area}",
                            jobId,
                            item.Weight,
                            item.Area);
                        
                        item.AppliedRules = new List<string> { "ERROR" };
                    }

                SafeUpdate(jobRepo, job, j =>
                {
                    j.Status = JobStatus.Completed;
                    j.CompletedAt = DateTime.UtcNow;
                    j.ErrorMessage = null;
                });

                _logger.LogInformation("Job completed: {JobId}", jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error processing job {JobId}", jobId);

                await HandleRetry(jobId, stoppingToken);
            }
        }
    }

    private async Task HandleRetry(string jobId, CancellationToken stoppingToken)
    {
        using var scope = _sp.CreateScope();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var job = jobRepo.Get(jobId);
        if (job == null) return;

        job.RetryCount++;
        job.ErrorMessage = "Processing failed";

        if (job.RetryCount <= _settings.MaxRetries)
        {
            job.Status = JobStatus.Pending;
            
            var baseDelay = Math.Pow(2, job.RetryCount);
            var jitter = Random.Shared.Next(0, 1000) / 1000.0;

            var delay = TimeSpan.FromSeconds(baseDelay + jitter);

            _logger.LogWarning(
                "Retrying job {JobId} in {Delay}s (Retry={Retry})",
                jobId,
                delay.TotalSeconds,
                job.RetryCount);

            SafeUpdate(jobRepo, job, _ => { });

            try
            {
                await Task.Delay(delay, stoppingToken);
                _queue.Enqueue(jobId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Retry canceled due to shutdown: {JobId}", jobId);
            }
        }
        else
        {
            job.Status = JobStatus.Failed;

            SafeUpdate(jobRepo, job, _ => { });

            _logger.LogError("Job permanently failed: {JobId}", jobId);
        }
    }

    private void SafeUpdate(IJobRepository repo, PricingJob job, Action<PricingJob> update)
    {
        try
        {
            update(job);
            repo.Update(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job state: {JobId}", job.JobId);
        }
    }
}