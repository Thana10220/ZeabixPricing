namespace Pricing.Application.Interfaces;

public interface IJobQueue
{
    void Enqueue(string jobId);
    Task<string> DequeueAsync(CancellationToken cancellationToken);
}