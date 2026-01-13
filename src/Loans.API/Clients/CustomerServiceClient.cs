using System.Net.Http.Json;

namespace Loans.API.Clients;

public class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerServiceClient> _logger;

    public CustomerServiceClient(HttpClient httpClient, ILogger<CustomerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CustomerDto?> GetCustomerAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CustomerApiResponse<CustomerDto>>(
                $"/api/customers/{customerId}");
            
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<CreditDto?> GetCustomerCreditAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CustomerApiResponse<CreditDto>>(
                $"/api/customers/{customerId}/credit");
            
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching credit for customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<bool> CustomerExistsAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/customers/{customerId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking customer existence {CustomerId}", customerId);
            return false;
        }
    }
}
