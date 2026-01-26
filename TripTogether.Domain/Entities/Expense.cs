

using TripTogether.Domain.Enums;

public class Expense : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid PaidBy { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    // Context & Details
    public string? Description { get; set; }
    public ExpenseCategory? Category { get; set; }
    public string? ReceiptImageUrl { get; set; }

    public DateTime ExpenseDate { get; set; }

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User Payer { get; set; } = null!;
    public virtual ICollection<ExpenseSplit> Splits { get; set; } = new List<ExpenseSplit>();
}
