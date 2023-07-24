namespace Groceries.Data;

using System.Text.Json.Serialization;

public class TransactionPromotion
{
    [JsonConstructor]
    public TransactionPromotion(Guid id, Guid transactionId, string name, decimal amount)
    {
        Id = id;
        TransactionId = transactionId;
        Name = name;
        Amount = amount;
    }

    public TransactionPromotion(string name, decimal amount) : this(Guid.NewGuid(), default, name, amount)
    {
    }

    public Guid Id { get; init; }
    public Guid TransactionId { get; init; }
    public string Name { get; set; }
    public decimal Amount { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();

    public Transaction? Transaction { get; init; }
}
