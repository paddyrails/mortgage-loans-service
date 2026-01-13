using System.ComponentModel.DataAnnotations;
using Loans.API.Models;
using Loans.API.Clients;

namespace Loans.API.DTOs;

// Loan Response with enriched data from other services
public record LoanResponseDto
{
    public Guid Id { get; init; }
    public string LoanNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public Guid PropertyId { get; init; }
    
    // Enriched from Customer Service
    public CustomerDto? Customer { get; init; }
    
    // Enriched from Property Service
    public PropertyDto? Property { get; init; }
    
    public decimal PrincipalAmount { get; init; }
    public decimal InterestRate { get; init; }
    public int TermMonths { get; init; }
    public string LoanType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal MonthlyPayment { get; init; }
    public decimal CurrentBalance { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? MaturityDate { get; init; }
    public decimal? DownPayment { get; init; }
    public decimal? LTV { get; init; }
    public decimal? DTI { get; init; }
    public bool HasEscrow { get; init; }
    public decimal EscrowBalance { get; init; }
    public decimal MonthlyEscrowAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Notes { get; init; }
}

// Simple response without enriched data
public record LoanSummaryDto
{
    public Guid Id { get; init; }
    public string LoanNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public Guid PropertyId { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestRate { get; init; }
    public int TermMonths { get; init; }
    public string LoanType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal MonthlyPayment { get; init; }
    public decimal CurrentBalance { get; init; }
    public DateTime CreatedAt { get; init; }
}

// Create Loan DTO
public record CreateLoanDto
{
    [Required]
    public Guid CustomerId { get; init; }

    [Required]
    public Guid PropertyId { get; init; }

    [Required]
    [Range(10000, 10000000)]
    public decimal PrincipalAmount { get; init; }

    [Required]
    [Range(0.001, 20)]
    public decimal InterestRate { get; init; }

    [Required]
    [Range(12, 480)]
    public int TermMonths { get; init; }

    [Required]
    public LoanType LoanType { get; init; }

    [Range(0, 10000000)]
    public decimal? DownPayment { get; init; }

    public bool HasEscrow { get; init; } = true;

    [Range(0, 10000)]
    public decimal? MonthlyEscrowAmount { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}

// Update Loan DTO
public record UpdateLoanDto
{
    public LoanStatus? Status { get; init; }
    public decimal? InterestRate { get; init; }
    public decimal? MonthlyEscrowAmount { get; init; }
    public string? Notes { get; init; }
}

// Fund Loan DTO
public record FundLoanDto
{
    [Required]
    public DateTime FundingDate { get; init; }

    [Required]
    public DateTime FirstPaymentDate { get; init; }

    public string? Notes { get; init; }
}

// Loan Balance DTO
public record LoanBalanceDto
{
    public Guid LoanId { get; init; }
    public string LoanNumber { get; init; } = string.Empty;
    public decimal OriginalBalance { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal PrincipalPaid { get; init; }
    public decimal InterestPaid { get; init; }
    public decimal EscrowBalance { get; init; }
    public int PaymentsMade { get; init; }
    public int PaymentsRemaining { get; init; }
    public DateTime? NextPaymentDate { get; init; }
    public decimal NextPaymentAmount { get; init; }
    public DateTime AsOfDate { get; init; }
}

// Amortization Schedule Item DTO
public record AmortizationItemDto
{
    public int PaymentNumber { get; init; }
    public DateTime PaymentDate { get; init; }
    public decimal PaymentAmount { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestAmount { get; init; }
    public decimal EscrowAmount { get; init; }
    public decimal RemainingBalance { get; init; }
    public decimal CumulativeInterest { get; init; }
    public decimal CumulativePrincipal { get; init; }
    public bool IsPaid { get; init; }
}

// API Response wrapper
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public List<string>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}
