using Loans.API.DTOs;
using Loans.API.Models;

namespace Loans.API.Services;

public interface ILoanService
{
    Task<IEnumerable<LoanSummaryDto>> GetAllLoansAsync();
    Task<LoanResponseDto?> GetLoanByIdAsync(Guid id, bool enrichData = true);
    Task<LoanResponseDto?> GetLoanByNumberAsync(string loanNumber);
    Task<IEnumerable<LoanSummaryDto>> GetLoansByCustomerAsync(Guid customerId);
    Task<IEnumerable<LoanSummaryDto>> GetLoansByPropertyAsync(Guid propertyId);
    Task<LoanResponseDto> CreateLoanAsync(CreateLoanDto dto);
    Task<LoanResponseDto?> UpdateLoanAsync(Guid id, UpdateLoanDto dto);
    Task<LoanResponseDto?> FundLoanAsync(Guid id, FundLoanDto dto);
    Task<bool> DeleteLoanAsync(Guid id);
    
    // Balance and Schedule
    Task<LoanBalanceDto?> GetLoanBalanceAsync(Guid id);
    Task<IEnumerable<AmortizationItemDto>> GetAmortizationScheduleAsync(Guid id);
    
    // Make payment (called by Payment Service)
    Task<bool> ApplyPaymentAsync(Guid loanId, decimal principalAmount, decimal interestAmount);
}
