namespace Groceries.Data;

public class List
{
    public List(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public List(string name) : this(default, name)
    {
    }

    public Guid Id { get; init; }
    public DateTime UpdatedAt { get; set; }
    public string Name { get; set; }

    public ICollection<ListItem>? Items { get; init; }
}
