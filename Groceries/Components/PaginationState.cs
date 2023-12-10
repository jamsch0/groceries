namespace Groceries.Components;

public class PaginationState
{
    private int itemCount;

    public int PageSize { get; set; } = 10;
    public int CurrentPage { get; internal set; }
    public int TotalItemCount { get; internal set; }
    public int ItemCount
    {
        get => itemCount;
        internal set
        {
            itemCount = value;
            ItemCountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int Offset => (CurrentPage - 1) * PageSize;
    public int LastPage => ((TotalItemCount - 1) / PageSize) + 1;

    internal event EventHandler? ItemCountChanged;

    public override int GetHashCode()
        => HashCode.Combine(PageSize, CurrentPage, ItemCount, TotalItemCount);
}
