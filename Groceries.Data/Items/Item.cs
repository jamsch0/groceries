namespace Groceries.Data;

using System.Text.Json.Serialization;

public class Item
{
    [JsonConstructor]
    public Item(Guid id, string brand, string name)
    {
        Id = id;
        Brand = brand;
        Name = name;
    }

    public Item(Guid id) : this(id, default!, default!)
    {
    }

    public Item(string brand, string name) : this(default, brand, name)
    {
    }

    public Guid Id { get; init; }
    public DateTime UpdatedAt { get; set; }
    public string Brand { get; set; }
    public string Name { get; set; }

    public ICollection<ItemBarcode> Barcodes { get; init; } = new List<ItemBarcode>();
    public IEnumerable<TransactionPromotion>? TransactionPromotions { get; init; }
}
