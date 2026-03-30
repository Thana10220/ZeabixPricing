namespace Pricing.Application.DTOs;

public class RemoteAreaConfig
{
    public List<string> Areas { get; set; } = new();
    public decimal Price { get; set; }
}