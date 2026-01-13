using Microsoft.AspNetCore.Mvc;
using Loans.API.Clients;

namespace Loans.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ICustomerServiceClient _customerClient;
    private readonly IPropertyServiceClient _propertyClient;

    public HealthController(ICustomerServiceClient customerClient, IPropertyServiceClient propertyClient)
    {
        _customerClient = customerClient;
        _propertyClient = propertyClient;
    }

    [HttpGet]
    public IActionResult Health() => Ok(new 
    { 
        Status = "Healthy", 
        Service = "Loans.API", 
        Timestamp = DateTime.UtcNow,
        Dependencies = new[] { "Customer.API", "Property.API" }
    });

    [HttpGet("live")]
    public IActionResult Live() => Ok(new { Status = "Alive" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        // Check dependencies
        var customerHealthy = await CheckCustomerServiceAsync();
        var propertyHealthy = await CheckPropertyServiceAsync();

        var status = new
        {
            Status = customerHealthy && propertyHealthy ? "Ready" : "Degraded",
            Dependencies = new
            {
                CustomerService = customerHealthy ? "Healthy" : "Unhealthy",
                PropertyService = propertyHealthy ? "Healthy" : "Unhealthy"
            }
        };

        return customerHealthy && propertyHealthy ? Ok(status) : StatusCode(503, status);
    }

    private async Task<bool> CheckCustomerServiceAsync()
    {
        try
        {
            // Try to check if a dummy customer exists (will return false but connection works)
            await _customerClient.CustomerExistsAsync(Guid.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckPropertyServiceAsync()
    {
        try
        {
            await _propertyClient.PropertyExistsAsync(Guid.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
