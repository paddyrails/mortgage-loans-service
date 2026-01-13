namespace Loans.API.Clients;

public interface IPropertyServiceClient
{
    Task<PropertyDto?> GetPropertyAsync(Guid propertyId);
    Task<AppraisalDto?> GetPropertyAppraisalAsync(Guid propertyId);
    Task<bool> PropertyExistsAsync(Guid propertyId);
}

// DTOs for Property Service responses
public record PropertyDto
{
    public Guid Id { get; init; }
    public string FullAddress { get; init; } = string.Empty;
    public string PropertyType { get; init; } = string.Empty;
    public decimal EstimatedValue { get; init; }
    public decimal ListingPrice { get; init; }
    public int Bedrooms { get; init; }
    public decimal Bathrooms { get; init; }
    public decimal SquareFeet { get; init; }
}

public record AppraisalDto
{
    public Guid Id { get; init; }
    public decimal AppraisedValue { get; init; }
    public DateTime AppraisalDate { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record PropertyApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
}
