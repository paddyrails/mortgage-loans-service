using Microsoft.EntityFrameworkCore;
using Loans.API.Models;

namespace Loans.API.Data;

public class LoanDbContext : DbContext
{
    public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options)
    {
    }

    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<AmortizationScheduleItem> AmortizationScheduleItems => Set<AmortizationScheduleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LoanNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.PropertyId);

            entity.HasMany(e => e.AmortizationSchedule)
                .WithOne(a => a.Loan)
                .HasForeignKey(a => a.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var loanId = Guid.Parse("11110000-0000-0000-0000-000000000001");
        
        // Using Customer and Property IDs from other services' seed data
        var customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var propertyId = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");

        modelBuilder.Entity<Loan>().HasData(
            new Loan
            {
                Id = loanId,
                LoanNumber = "LN-2024-000001",
                CustomerId = customerId,
                PropertyId = propertyId,
                PrincipalAmount = 680000,
                InterestRate = 6.875m,
                TermMonths = 360,
                LoanType = LoanType.Conventional,
                Status = LoanStatus.Active,
                MonthlyPayment = 4465.27m,
                CurrentBalance = 678500,
                OriginalBalance = 680000,
                StartDate = DateTime.UtcNow.AddMonths(-3),
                MaturityDate = DateTime.UtcNow.AddYears(30).AddMonths(-3),
                FirstPaymentDate = DateTime.UtcNow.AddMonths(-2),
                DownPayment = 170000,
                LTV = 80.0m,
                DTI = 35.5m,
                HasEscrow = true,
                EscrowBalance = 2400,
                MonthlyEscrowAmount = 800,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            }
        );
    }
}
