namespace Groceries.Data;

using System.Text.Json.Serialization;

public class TransactionItem
{
    [JsonConstructor]
    public TransactionItem(Guid transactionId, Guid itemId, decimal price, decimal quantity, string? unit)
    {
        TransactionId = transactionId;
        ItemId = itemId;
        Price = price;
        Quantity = quantity;
        Unit = unit;
    }

    public TransactionItem(Guid itemId, decimal price, decimal quantity, string? unit) : this(default, itemId, price, quantity, unit)
    {
    }

    public Guid TransactionId { get; init; }
    public Guid ItemId { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }

    public Item? Item { get; set; }

    public decimal Amount => Price * Quantity;
}
