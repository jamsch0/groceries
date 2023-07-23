namespace Groceries.Data;

public class TransactionTotal
{
    public Guid TransactionId { get; init; }
    public decimal Total { get; init; }

    public Transaction? Transaction { get; init; }
}
