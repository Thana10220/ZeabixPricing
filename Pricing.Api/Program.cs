using Pricing.Api.Middlewares;
using Pricing.Application;
using Pricing.Infrastructure;

using Pricing.Application.Interfaces;
using Pricing.Application.Services;
using Pricing.Infrastructure.Configuration;
using Pricing.Infrastructure.Persistence;
using Pricing.Infrastructure.Queue;
using Pricing.Infrastructure.Workers;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<JobSettings>( builder.Configuration.GetSection("JobSettings"));
builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
builder.Services.AddSingleton<IJobQueue, InMemoryJobQueue>();
builder.Services.AddHostedService<JobWorker>();
builder.Services.AddScoped<IBulkQuoteService, BulkQuoteService>();
builder.Services.AddScoped<ICsvParser, CsvParser>();
builder.Services.AddSingleton<IRuleService, RuleService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);   
        opt.PermitLimit = 20;                    
        opt.QueueLimit = 5;                     
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    
    options.AddFixedWindowLimiter("bulk", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(30);
        opt.PermitLimit = 5;
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>(); 
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRateLimiter();
app.MapGet("/health", () => "OK");
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}