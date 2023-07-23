namespace Groceries.Data;

public class Store
{
    public Store(Guid id, Guid retailerId, string name, string? address = null)
    {
        Id = id;
        RetailerId = retailerId;
        Name = name;
        Address = address;
    }

    public Store(Guid retailerId, string name, string? address = null)
        : this(default, retailerId, name, address)
    {
    }

    public Guid Id { get; init; }
    public Guid RetailerId { get; init; }
    public string Name { get; set; }
    public string? Address { get; set; }

    public Retailer? Retailer { get; init; }
    public IEnumerable<Transaction>? Transactions { get; init; }
}
