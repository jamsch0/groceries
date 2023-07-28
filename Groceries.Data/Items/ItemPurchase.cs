namespace Groceries.Data;

public class ItemPurchase
{
    public Guid ItemId { get; init; }
    public Guid TransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid StoreId { get; init; }
    public decimal Price { get; init; }
    public int Quantity { get; init; }
    public bool IsLastPurchase { get; init; }

    public Item? Item { get; init; }
    public Transaction? Transaction { get; init; }
    public Store? Store { get; init; }
}
