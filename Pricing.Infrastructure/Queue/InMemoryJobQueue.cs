namespace Pricing.Infrastructure.Queue;

using Pricing.Application.Interfaces;
using System.Threading.Channels;

public class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();

    public void Enqueue(string jobId)
    {
        _queue.Writer.TryWrite(jobId);
    }

    public async Task<string> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}