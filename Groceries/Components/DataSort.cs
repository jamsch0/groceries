namespace Groceries.Components;

using System.Linq.Expressions;

public static class DataSort
{
    public static DataSort<TItem> By<TItem, TResult>(Expression<Func<TItem, TResult>> expression, string key)
        => new((source, desc) => desc ? source.OrderByDescending(expression) : source.OrderBy(expression), key);

    public static DataSort<TItem> ByDescending<TItem, TResult>(Expression<Func<TItem, TResult>> expression, string key)
        => new((source, desc) => desc ? source.OrderBy(expression) : source.OrderByDescending(expression), key);
}

public class DataSort<TItem>
{
    private readonly Func<IQueryable<TItem>, bool, IOrderedQueryable<TItem>> first;
    private readonly List<Func<IOrderedQueryable<TItem>, bool, IOrderedQueryable<TItem>>> thens = [];

    internal DataSort(Func<IQueryable<TItem>, bool, IOrderedQueryable<TItem>> first, string key)
    {
        this.first = first;
        Key = key;
    }

    public string Key { get; }

    public DataSort<TItem> ThenBy<TResult>(Expression<Func<TItem, TResult>> expression)
    {
        thens.Add((source, desc) => desc ? source.ThenByDescending(expression) : source.ThenBy(expression));
        return this;
    }

    public DataSort<TItem> ThenByDescending<TResult>(Expression<Func<TItem, TResult>> expression)
    {
        thens.Add((source, desc) => desc ? source.ThenBy(expression) : source.ThenByDescending(expression));
        return this;
    }

    internal IOrderedQueryable<TItem> Apply(IQueryable<TItem> source, bool descending)
    {
        var ordered = first(source, descending);
        foreach (var then in thens)
        {
            ordered = then(ordered, descending);
        }

        return ordered;
    }
}
