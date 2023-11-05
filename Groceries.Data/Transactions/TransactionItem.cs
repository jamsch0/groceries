namespace Groceries.Data;

using System.Text.Json.Serialization;

public class TransactionItem
{
    [JsonConstructor]
    public TransactionItem(Guid transactionId, Guid itemId, decimal price, int quantity)
    {
        TransactionId = transactionId;
        ItemId = itemId;
        Price = price;
        Quantity = quantity;
    }

    public TransactionItem(Guid itemId, decimal price, int quantity) : this(default, itemId, price, quantity)
    {
    }

    public Guid TransactionId { get; init; }
    public Guid ItemId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public Item? Item { get; set; }

    public decimal Amount => Price * Quantity;
}
