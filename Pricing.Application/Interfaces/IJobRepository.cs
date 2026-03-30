namespace Pricing.Application.Interfaces;

using Pricing.Domain.Entities;

public interface IJobRepository
{
    void Create(PricingJob job);
    PricingJob Get(string jobId);
    void Update(PricingJob job);
}