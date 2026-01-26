

using TripTogether.Domain.Enums;

public class Settlement : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid PayerId { get; set; }
    public Guid PayeeId { get; set; }
    public decimal Amount { get; set; }
    public SettlementStatus Status { get; set; }
    public DateTime TransactionDate { get; set; }

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User Payer { get; set; } = null!;
    public virtual User Payee { get; set; } = null!;
}
