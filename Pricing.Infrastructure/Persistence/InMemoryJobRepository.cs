namespace Pricing.Infrastructure.Persistence;

using Pricing.Application.Interfaces;
using Pricing.Domain.Entities;

public class InMemoryJobRepository : IJobRepository
{
    private readonly Dictionary<string, PricingJob> _jobs = new();

    public void Create(PricingJob job)
    {
        _jobs[job.JobId] = job;
    }

    public PricingJob Get(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    public void Update(PricingJob job)
    {
        _jobs[job.JobId] = job;
    }
}