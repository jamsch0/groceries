namespace Groceries;

using Microsoft.EntityFrameworkCore;
using System.Collections;

public interface IListPageModel
{
    int Offset { get; }
    int Page { get; }
    int PageSize { get; }
    int LastPage { get; }
    int Total { get; }
    int Count { get; }
}

public record ListPageModel<TItem> : IListPageModel, IReadOnlyCollection<TItem>
{
    public ListPageModel(IList<TItem> items)
    {
        Items = items;
    }

    public int Offset { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int LastPage { get; init; }
    public int Total { get; init; }
    public IList<TItem> Items { get; init; }

    public int Count => Items.Count;

    public IEnumerator<TItem> GetEnumerator()
    {
        return Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Items.GetEnumerator();
    }
}

public static class ListPageModel
{
    public static ListPageModel<TItem> Empty<TItem>()
    {
        return new ListPageModel<TItem>(Array.Empty<TItem>());
    }
}

public static class ListPageModelExtensions
{
    public static async Task<ListPageModel<TItem>> ToListPageModelAsync<TItem>(
        this IQueryable<TItem> query,
        int page,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            return new ListPageModel<TItem>(Array.Empty<TItem>()) { Page = 1 };
        }

        var total = await query.CountAsync(cancellationToken);
        var lastPage = Math.Max(1, (int)Math.Ceiling((float)total / pageSize));

        if (page > lastPage)
        {
            return new ListPageModel<TItem>(Array.Empty<TItem>()) { Page = lastPage };
        }

        var offset = (page - 1) * pageSize;

        var items = await query
            .Skip(offset)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new ListPageModel<TItem>(items)
        {
            Offset = offset,
            Page = page,
            PageSize = pageSize,
            LastPage = lastPage,
            Total = total,
        };
    }
}
