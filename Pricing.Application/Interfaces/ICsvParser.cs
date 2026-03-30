namespace Pricing.Application.Interfaces;

using Pricing.Application.DTOs;

public interface ICsvParser
{
    Task<List<QuoteRequest>> ParseAsync(Stream stream);
}