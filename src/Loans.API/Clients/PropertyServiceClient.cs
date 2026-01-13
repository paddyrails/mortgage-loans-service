using System.Net.Http.Json;

namespace Loans.API.Clients;

public class PropertyServiceClient : IPropertyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PropertyServiceClient> _logger;

    public PropertyServiceClient(HttpClient httpClient, ILogger<PropertyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PropertyDto?> GetPropertyAsync(Guid propertyId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<PropertyApiResponse<PropertyDto>>(
                $"/api/properties/{propertyId}");
            
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching property {PropertyId}", propertyId);
            return null;
        }
    }

    public async Task<AppraisalDto?> GetPropertyAppraisalAsync(Guid propertyId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<PropertyApiResponse<AppraisalDto>>(
                $"/api/properties/{propertyId}/appraisal");
            
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appraisal for property {PropertyId}", propertyId);
            return null;
        }
    }

    public async Task<bool> PropertyExistsAsync(Guid propertyId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/properties/{propertyId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking property existence {PropertyId}", propertyId);
            return false;
        }
    }
}
