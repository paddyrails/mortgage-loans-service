using Microsoft.AspNetCore.Mvc;
using Loans.API.DTOs;
using Loans.API.Services;

namespace Loans.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly ILogger<LoansController> _logger;

    public LoansController(ILoanService loanService, ILogger<LoansController> logger)
    {
        _loanService = loanService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoanSummaryDto>>>> GetAll()
    {
        var loans = await _loanService.GetAllLoansAsync();
        return Ok(ApiResponse<IEnumerable<LoanSummaryDto>>.SuccessResponse(loans));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LoanResponseDto>>> GetById(Guid id, [FromQuery] bool enrich = true)
    {
        var loan = await _loanService.GetLoanByIdAsync(id, enrich);
        if (loan == null)
            return NotFound(ApiResponse<LoanResponseDto>.FailResponse($"Loan {id} not found"));
        return Ok(ApiResponse<LoanResponseDto>.SuccessResponse(loan));
    }

    [HttpGet("number/{loanNumber}")]
    public async Task<ActionResult<ApiResponse<LoanResponseDto>>> GetByNumber(string loanNumber)
    {
        var loan = await _loanService.GetLoanByNumberAsync(loanNumber);
        if (loan == null)
            return NotFound(ApiResponse<LoanResponseDto>.FailResponse($"Loan {loanNumber} not found"));
        return Ok(ApiResponse<LoanResponseDto>.SuccessResponse(loan));
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoanSummaryDto>>>> GetByCustomer(Guid customerId)
    {
        var loans = await _loanService.GetLoansByCustomerAsync(customerId);
        return Ok(ApiResponse<IEnumerable<LoanSummaryDto>>.SuccessResponse(loans));
    }

    [HttpGet("property/{propertyId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoanSummaryDto>>>> GetByProperty(Guid propertyId)
    {
        var loans = await _loanService.GetLoansByPropertyAsync(propertyId);
        return Ok(ApiResponse<IEnumerable<LoanSummaryDto>>.SuccessResponse(loans));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LoanResponseDto>>> Create([FromBody] CreateLoanDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<LoanResponseDto>.FailResponse("Validation failed", errors));
        }

        try
        {
            var loan = await _loanService.CreateLoanAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = loan.Id },
                ApiResponse<LoanResponseDto>.SuccessResponse(loan, "Loan created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LoanResponseDto>.FailResponse(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LoanResponseDto>>> Update(Guid id, [FromBody] UpdateLoanDto dto)
    {
        var loan = await _loanService.UpdateLoanAsync(id, dto);
        if (loan == null)
            return NotFound(ApiResponse<LoanResponseDto>.FailResponse($"Loan {id} not found"));
        return Ok(ApiResponse<LoanResponseDto>.SuccessResponse(loan, "Loan updated"));
    }

    [HttpPost("{id:guid}/fund")]
    public async Task<ActionResult<ApiResponse<LoanResponseDto>>> Fund(Guid id, [FromBody] FundLoanDto dto)
    {
        var loan = await _loanService.FundLoanAsync(id, dto);
        if (loan == null)
            return NotFound(ApiResponse<LoanResponseDto>.FailResponse($"Loan {id} not found"));
        return Ok(ApiResponse<LoanResponseDto>.SuccessResponse(loan, "Loan funded"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var result = await _loanService.DeleteLoanAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.FailResponse($"Loan {id} not found"));
        return Ok(ApiResponse<object>.SuccessResponse(new { Id = id }, "Loan cancelled"));
    }

    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<ApiResponse<LoanBalanceDto>>> GetBalance(Guid id)
    {
        var balance = await _loanService.GetLoanBalanceAsync(id);
        if (balance == null)
            return NotFound(ApiResponse<LoanBalanceDto>.FailResponse($"Loan {id} not found"));
        return Ok(ApiResponse<LoanBalanceDto>.SuccessResponse(balance));
    }

    [HttpGet("{id:guid}/schedule")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AmortizationItemDto>>>> GetSchedule(Guid id)
    {
        var schedule = await _loanService.GetAmortizationScheduleAsync(id);
        return Ok(ApiResponse<IEnumerable<AmortizationItemDto>>.SuccessResponse(schedule));
    }

    // Internal endpoint for Payment Service
    [HttpPost("{id:guid}/apply-payment")]
    public async Task<ActionResult<ApiResponse<object>>> ApplyPayment(Guid id, 
        [FromQuery] decimal principal, [FromQuery] decimal interest)
    {
        var result = await _loanService.ApplyPaymentAsync(id, principal, interest);
        if (!result)
            return NotFound(ApiResponse<object>.FailResponse($"Loan {id} not found"));
        return Ok(ApiResponse<object>.SuccessResponse(new { Applied = true }, "Payment applied"));
    }
}
