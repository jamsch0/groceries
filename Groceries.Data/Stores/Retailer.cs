namespace Groceries.Data;

public class Retailer
{
    public Retailer(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Retailer(string name) : this(default, name)
    {
    }

    public Guid Id { get; init; }
    public string Name { get; set; }
}
