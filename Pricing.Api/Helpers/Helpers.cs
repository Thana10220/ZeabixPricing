namespace Pricing.Api.Helpers;

public static class CorrelationHelper
{
    public static string GetCorrelationId(HttpContext context)
    {
        return context.Items["CorrelationId"]?.ToString() ?? "N/A";
    }
}