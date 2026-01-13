using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Loans.API.Models;

public class AmortizationScheduleItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LoanId { get; set; }

    [Required]
    public int PaymentNumber { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaymentAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrincipalAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal InterestAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EscrowAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RemainingBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CumulativeInterest { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CumulativePrincipal { get; set; }

    public bool IsPaid { get; set; }

    public DateTime? ActualPaymentDate { get; set; }

    // Navigation
    public Loan? Loan { get; set; }
}
