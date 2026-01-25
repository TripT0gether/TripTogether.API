

public class ExpenseSplit : BaseEntity
{
    public Guid ExpenseId { get; set; }
    public Guid UserId { get; set; }
    public decimal AmountOwed { get; set; }
    public bool IsManualSplit { get; set; } = false;

    // Navigation properties
    public virtual Expense Expense { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
