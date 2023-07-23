namespace Groceries.Data;

public class ListItem
{
    public ListItem(Guid id, Guid listId, string name)
    {
        Id = id;
        ListId = listId;
        Name = name;
    }

    public ListItem(Guid listId, string name) : this(default, listId, name)
    {
    }

    public Guid Id { get; init; }
    public Guid ListId { get; init; }
    public string Name { get; set; }
    public bool Completed { get; set; }
}
