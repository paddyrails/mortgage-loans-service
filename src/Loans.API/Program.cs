using Microsoft.EntityFrameworkCore;
using Loans.API.Data;
using Loans.API.Services;
using Loans.API.Clients;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseInMemoryDatabase("LoanDb"));

// Configure HTTP Clients for dependent services with Polly retry policies
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Customer Service Client
builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Property Service Client
builder.Services.AddHttpClient<IPropertyServiceClient, PropertyServiceClient>(client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:PropertyService"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

// Add Services
builder.Services.AddScoped<ILoanService, LoanService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Loans Service API", 
        Version = "v1",
        Description = "Microservice for managing loans. Depends on Customer and Property services."
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
    context.Database.EnsureCreated();
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
app.Urls.Add($"http://+:{port}");

Console.WriteLine($"Loans Service starting on port {port}...");
Console.WriteLine("Dependencies:");
Console.WriteLine($"  - Customer Service: {builder.Configuration["ServiceUrls:CustomerService"] ?? "http://localhost:5001"}");
Console.WriteLine($"  - Property Service: {builder.Configuration["ServiceUrls:PropertyService"] ?? "http://localhost:5002"}");

app.Run();
