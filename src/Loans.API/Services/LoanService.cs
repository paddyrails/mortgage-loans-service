using Microsoft.EntityFrameworkCore;
using Loans.API.Data;
using Loans.API.DTOs;
using Loans.API.Models;
using Loans.API.Clients;

namespace Loans.API.Services;

public class LoanService : ILoanService
{
    private readonly LoanDbContext _context;
    private readonly ICustomerServiceClient _customerClient;
    private readonly IPropertyServiceClient _propertyClient;
    private readonly ILogger<LoanService> _logger;

    public LoanService(
        LoanDbContext context,
        ICustomerServiceClient customerClient,
        IPropertyServiceClient propertyClient,
        ILogger<LoanService> logger)
    {
        _context = context;
        _customerClient = customerClient;
        _propertyClient = propertyClient;
        _logger = logger;
    }

    public async Task<IEnumerable<LoanSummaryDto>> GetAllLoansAsync()
    {
        var loans = await _context.Loans.ToListAsync();
        return loans.Select(MapToSummaryDto);
    }

    public async Task<LoanResponseDto?> GetLoanByIdAsync(Guid id, bool enrichData = true)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return null;

        return await MapToResponseDtoAsync(loan, enrichData);
    }

    public async Task<LoanResponseDto?> GetLoanByNumberAsync(string loanNumber)
    {
        var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);
        if (loan == null) return null;

        return await MapToResponseDtoAsync(loan, true);
    }

    public async Task<IEnumerable<LoanSummaryDto>> GetLoansByCustomerAsync(Guid customerId)
    {
        var loans = await _context.Loans
            .Where(l => l.CustomerId == customerId)
            .ToListAsync();
        return loans.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<LoanSummaryDto>> GetLoansByPropertyAsync(Guid propertyId)
    {
        var loans = await _context.Loans
            .Where(l => l.PropertyId == propertyId)
            .ToListAsync();
        return loans.Select(MapToSummaryDto);
    }

    public async Task<LoanResponseDto> CreateLoanAsync(CreateLoanDto dto)
    {
        // Validate customer exists
        var customerExists = await _customerClient.CustomerExistsAsync(dto.CustomerId);
        if (!customerExists)
        {
            throw new InvalidOperationException($"Customer {dto.CustomerId} not found");
        }

        // Validate property exists
        var propertyExists = await _propertyClient.PropertyExistsAsync(dto.PropertyId);
        if (!propertyExists)
        {
            throw new InvalidOperationException($"Property {dto.PropertyId} not found");
        }

        // Get property value for LTV calculation
        var property = await _propertyClient.GetPropertyAsync(dto.PropertyId);
        var propertyValue = property?.EstimatedValue ?? property?.ListingPrice ?? dto.PrincipalAmount;

        // Calculate monthly payment
        var monthlyPayment = CalculateMonthlyPayment(dto.PrincipalAmount, dto.InterestRate, dto.TermMonths);

        // Calculate LTV
        var ltv = (dto.PrincipalAmount / propertyValue) * 100;

        var loan = new Loan
        {
            LoanNumber = await GenerateLoanNumberAsync(),
            CustomerId = dto.CustomerId,
            PropertyId = dto.PropertyId,
            PrincipalAmount = dto.PrincipalAmount,
            InterestRate = dto.InterestRate,
            TermMonths = dto.TermMonths,
            LoanType = dto.LoanType,
            Status = LoanStatus.Pending,
            MonthlyPayment = monthlyPayment,
            CurrentBalance = dto.PrincipalAmount,
            OriginalBalance = dto.PrincipalAmount,
            DownPayment = dto.DownPayment,
            LTV = ltv,
            HasEscrow = dto.HasEscrow,
            MonthlyEscrowAmount = dto.MonthlyEscrowAmount ?? 0,
            Notes = dto.Notes
        };

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created loan {LoanNumber} for customer {CustomerId}", loan.LoanNumber, loan.CustomerId);

        return await MapToResponseDtoAsync(loan, true);
    }

    public async Task<LoanResponseDto?> UpdateLoanAsync(Guid id, UpdateLoanDto dto)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null) return null;

        if (dto.Status.HasValue) loan.Status = dto.Status.Value;
        if (dto.InterestRate.HasValue)
        {
            loan.InterestRate = dto.InterestRate.Value;
            loan.MonthlyPayment = CalculateMonthlyPayment(loan.CurrentBalance, dto.InterestRate.Value, 
                GetRemainingMonths(loan));
        }
        if (dto.MonthlyEscrowAmount.HasValue) loan.MonthlyEscrowAmount = dto.MonthlyEscrowAmount.Value;
        if (dto.Notes != null) loan.Notes = dto.Notes;

        loan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await MapToResponseDtoAsync(loan, true);
    }

    public async Task<LoanResponseDto?> FundLoanAsync(Guid id, FundLoanDto dto)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null) return null;

        loan.Status = LoanStatus.Funded;
        loan.StartDate = dto.FundingDate;
        loan.FirstPaymentDate = dto.FirstPaymentDate;
        loan.MaturityDate = dto.FirstPaymentDate.AddMonths(loan.TermMonths);
        loan.UpdatedAt = DateTime.UtcNow;
        if (dto.Notes != null) loan.Notes = dto.Notes;

        // Generate amortization schedule
        await GenerateAmortizationScheduleAsync(loan);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Funded loan {LoanNumber}", loan.LoanNumber);

        return await MapToResponseDtoAsync(loan, true);
    }

    public async Task<bool> DeleteLoanAsync(Guid id)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan == null) return false;

        loan.Status = LoanStatus.Cancelled;
        loan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<LoanBalanceDto?> GetLoanBalanceAsync(Guid id)
    {
        var loan = await _context.Loans
            .Include(l => l.AmortizationSchedule)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null) return null;

        var paidItems = loan.AmortizationSchedule.Where(a => a.IsPaid).ToList();
        var nextPayment = loan.AmortizationSchedule
            .Where(a => !a.IsPaid)
            .OrderBy(a => a.PaymentDate)
            .FirstOrDefault();

        return new LoanBalanceDto
        {
            LoanId = loan.Id,
            LoanNumber = loan.LoanNumber,
            OriginalBalance = loan.OriginalBalance,
            CurrentBalance = loan.CurrentBalance,
            PrincipalPaid = paidItems.Sum(a => a.PrincipalAmount),
            InterestPaid = paidItems.Sum(a => a.InterestAmount),
            EscrowBalance = loan.EscrowBalance,
            PaymentsMade = paidItems.Count,
            PaymentsRemaining = loan.AmortizationSchedule.Count(a => !a.IsPaid),
            NextPaymentDate = nextPayment?.PaymentDate,
            NextPaymentAmount = nextPayment?.PaymentAmount ?? loan.MonthlyPayment,
            AsOfDate = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<AmortizationItemDto>> GetAmortizationScheduleAsync(Guid id)
    {
        var loan = await _context.Loans
            .Include(l => l.AmortizationSchedule)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null) return Enumerable.Empty<AmortizationItemDto>();

        return loan.AmortizationSchedule
            .OrderBy(a => a.PaymentNumber)
            .Select(a => new AmortizationItemDto
            {
                PaymentNumber = a.PaymentNumber,
                PaymentDate = a.PaymentDate,
                PaymentAmount = a.PaymentAmount,
                PrincipalAmount = a.PrincipalAmount,
                InterestAmount = a.InterestAmount,
                EscrowAmount = a.EscrowAmount,
                RemainingBalance = a.RemainingBalance,
                CumulativeInterest = a.CumulativeInterest,
                CumulativePrincipal = a.CumulativePrincipal,
                IsPaid = a.IsPaid
            });
    }

    public async Task<bool> ApplyPaymentAsync(Guid loanId, decimal principalAmount, decimal interestAmount)
    {
        var loan = await _context.Loans.FindAsync(loanId);
        if (loan == null) return false;

        loan.CurrentBalance -= principalAmount;
        loan.UpdatedAt = DateTime.UtcNow;

        // Mark next schedule item as paid
        var nextItem = await _context.AmortizationScheduleItems
            .Where(a => a.LoanId == loanId && !a.IsPaid)
            .OrderBy(a => a.PaymentNumber)
            .FirstOrDefaultAsync();

        if (nextItem != null)
        {
            nextItem.IsPaid = true;
            nextItem.ActualPaymentDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #region Private Helper Methods

    private async Task<string> GenerateLoanNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Loans.CountAsync(l => l.LoanNumber.StartsWith($"LN-{year}"));
        return $"LN-{year}-{(count + 1):D6}";
    }

    private static decimal CalculateMonthlyPayment(decimal principal, decimal annualRate, int termMonths)
    {
        if (termMonths <= 0 || principal <= 0) return 0;
        if (annualRate <= 0) return principal / termMonths;

        var monthlyRate = annualRate / 100 / 12;
        var payment = principal * (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), termMonths))
                      / ((decimal)Math.Pow((double)(1 + monthlyRate), termMonths) - 1);
        return Math.Round(payment, 2);
    }

    private static int GetRemainingMonths(Loan loan)
    {
        if (!loan.MaturityDate.HasValue) return loan.TermMonths;
        var remaining = (loan.MaturityDate.Value.Year - DateTime.UtcNow.Year) * 12 +
                       (loan.MaturityDate.Value.Month - DateTime.UtcNow.Month);
        return Math.Max(1, remaining);
    }

    private async Task GenerateAmortizationScheduleAsync(Loan loan)
    {
        // Remove existing schedule
        var existingItems = await _context.AmortizationScheduleItems
            .Where(a => a.LoanId == loan.Id)
            .ToListAsync();
        _context.AmortizationScheduleItems.RemoveRange(existingItems);

        var balance = loan.PrincipalAmount;
        var monthlyRate = loan.InterestRate / 100 / 12;
        var paymentDate = loan.FirstPaymentDate ?? DateTime.UtcNow.AddMonths(1);
        decimal cumulativeInterest = 0;
        decimal cumulativePrincipal = 0;

        for (int i = 1; i <= loan.TermMonths && balance > 0; i++)
        {
            var interestAmount = Math.Round(balance * monthlyRate, 2);
            var principalAmount = Math.Min(loan.MonthlyPayment - interestAmount, balance);
            balance -= principalAmount;

            cumulativeInterest += interestAmount;
            cumulativePrincipal += principalAmount;

            var scheduleItem = new AmortizationScheduleItem
            {
                LoanId = loan.Id,
                PaymentNumber = i,
                PaymentDate = paymentDate,
                PaymentAmount = loan.MonthlyPayment + loan.MonthlyEscrowAmount,
                PrincipalAmount = principalAmount,
                InterestAmount = interestAmount,
                EscrowAmount = loan.MonthlyEscrowAmount,
                RemainingBalance = Math.Max(0, balance),
                CumulativeInterest = cumulativeInterest,
                CumulativePrincipal = cumulativePrincipal,
                IsPaid = false
            };

            _context.AmortizationScheduleItems.Add(scheduleItem);
            paymentDate = paymentDate.AddMonths(1);
        }
    }

    private async Task<LoanResponseDto> MapToResponseDtoAsync(Loan loan, bool enrichData)
    {
        CustomerDto? customer = null;
        PropertyDto? property = null;

        if (enrichData)
        {
            // Fetch data from other services in parallel
            var customerTask = _customerClient.GetCustomerAsync(loan.CustomerId);
            var propertyTask = _propertyClient.GetPropertyAsync(loan.PropertyId);

            await Task.WhenAll(customerTask, propertyTask);

            customer = await customerTask;
            property = await propertyTask;
        }

        return new LoanResponseDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            CustomerId = loan.CustomerId,
            PropertyId = loan.PropertyId,
            Customer = customer,
            Property = property,
            PrincipalAmount = loan.PrincipalAmount,
            InterestRate = loan.InterestRate,
            TermMonths = loan.TermMonths,
            LoanType = loan.LoanType.ToString(),
            Status = loan.Status.ToString(),
            MonthlyPayment = loan.MonthlyPayment,
            CurrentBalance = loan.CurrentBalance,
            StartDate = loan.StartDate,
            MaturityDate = loan.MaturityDate,
            DownPayment = loan.DownPayment,
            LTV = loan.LTV,
            DTI = loan.DTI,
            HasEscrow = loan.HasEscrow,
            EscrowBalance = loan.EscrowBalance,
            MonthlyEscrowAmount = loan.MonthlyEscrowAmount,
            CreatedAt = loan.CreatedAt,
            Notes = loan.Notes
        };
    }

    private static LoanSummaryDto MapToSummaryDto(Loan loan)
    {
        return new LoanSummaryDto
        {
            Id = loan.Id,
            LoanNumber = loan.LoanNumber,
            CustomerId = loan.CustomerId,
            PropertyId = loan.PropertyId,
            PrincipalAmount = loan.PrincipalAmount,
            InterestRate = loan.InterestRate,
            TermMonths = loan.TermMonths,
            LoanType = loan.LoanType.ToString(),
            Status = loan.Status.ToString(),
            MonthlyPayment = loan.MonthlyPayment,
            CurrentBalance = loan.CurrentBalance,
            CreatedAt = loan.CreatedAt
        };
    }

    #endregion
}
