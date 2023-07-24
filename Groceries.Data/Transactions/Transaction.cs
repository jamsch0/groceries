namespace Groceries.Data;

using System.Text.Json.Serialization;

public class Transaction
{
    [JsonConstructor]
    public Transaction(Guid id, DateTime createdAt, Guid storeId)
    {
        Id = id;
        CreatedAt = createdAt;
        StoreId = storeId;
    }

    public Transaction(DateTime createdAt, Guid storeId) : this(default, createdAt, storeId)
    {
    }

    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid StoreId { get; init; }

    public ICollection<TransactionItem> Items { get; init; } = new List<TransactionItem>();
    public ICollection<TransactionPromotion> Promotions { get; init; } = new List<TransactionPromotion>();

    public Store? Store { get; init; }

    public decimal Total => Items.Sum(item => item.Price * item.Quantity) - Promotions.Sum(promotion => promotion.Amount);
}
