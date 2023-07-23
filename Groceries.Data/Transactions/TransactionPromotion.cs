namespace Groceries.Data;

public class TransactionPromotion
{
    public TransactionPromotion(Guid id, Guid transactionId, string name, decimal amount)
    {
        Id = id;
        TransactionId = transactionId;
        Name = name;
        Amount = amount;
    }

    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Amount { get; set; }

    public ICollection<Item> Items { get; init; } = new List<Item>();

    public Transaction? Transaction { get; init; }
}
