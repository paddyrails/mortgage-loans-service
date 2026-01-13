using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Loans.API.Models;

public class Loan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(20)]
    public string LoanNumber { get; set; } = string.Empty;

    // Foreign keys to other services (stored as IDs, fetched via HTTP)
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public Guid PropertyId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,3)")]
    public decimal InterestRate { get; set; }

    [Required]
    public int TermMonths { get; set; }

    [Required]
    public LoanType LoanType { get; set; }

    [Required]
    public LoanStatus Status { get; set; } = LoanStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyPayment { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OriginalBalance { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? MaturityDate { get; set; }

    public DateTime? FirstPaymentDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DownPayment { get; set; }

    [Column(TypeName = "decimal(5,3)")]
    public decimal? LTV { get; set; }  // Loan to Value ratio

    [Column(TypeName = "decimal(5,3)")]
    public decimal? DTI { get; set; }  // Debt to Income ratio

    // Escrow
    public bool HasEscrow { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    public decimal EscrowBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyEscrowAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation
    public List<AmortizationScheduleItem> AmortizationSchedule { get; set; } = new();
}

public enum LoanType
{
    Conventional = 1,
    FHA = 2,
    VA = 3,
    USDA = 4,
    Jumbo = 5,
    ARM = 6,  // Adjustable Rate Mortgage
    InterestOnly = 7
}

public enum LoanStatus
{
    Pending = 1,
    Approved = 2,
    Funded = 3,
    Active = 4,
    Delinquent = 5,
    Default = 6,
    PaidOff = 7,
    Foreclosure = 8,
    Cancelled = 9
}
