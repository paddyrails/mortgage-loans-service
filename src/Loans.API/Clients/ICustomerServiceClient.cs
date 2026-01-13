namespace Loans.API.Clients;

public interface ICustomerServiceClient
{
    Task<CustomerDto?> GetCustomerAsync(Guid customerId);
    Task<CreditDto?> GetCustomerCreditAsync(Guid customerId);
    Task<bool> CustomerExistsAsync(Guid customerId);
}

// DTOs for Customer Service responses
public record CustomerDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}

public record CreditDto
{
    public Guid Id { get; init; }
    public int CreditScore { get; init; }
    public string CreditRating { get; init; } = string.Empty;
    public decimal TotalDebt { get; init; }
    public decimal AvailableCredit { get; init; }
}

public record CustomerApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
}
