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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>(); 
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapGet("/health", () => "OK");
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}