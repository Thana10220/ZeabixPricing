namespace Pricing.Application.DTOs;

public class TimeWindowConfig
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public decimal Discount { get; set; }
}