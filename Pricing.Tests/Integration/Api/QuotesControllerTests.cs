using System.Net.Http.Json;

namespace Pricing.Tests.Integration.Api;

public class QuotesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public QuotesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Bulk_Should_Create_Job()
    {
        var response = await _client.PostAsJsonAsync("/quotes/bulk", new[]
        {
            new { weight = 10, area = "เชียงใหม่", requestTime = DateTime.UtcNow }
        });

        response.EnsureSuccessStatusCode();
    }
}