namespace Groceries.Components;

using Microsoft.AspNetCore.Components;

public abstract class TableColumn<TItem> : ComponentBase
{
    [CascadingParameter]
    private Table<TItem> Table { get; set; } = default!;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public Align? Align { get; set; }

    [Parameter]
    public bool Fill { get; set; }

    [Parameter]
    public RenderFragment HeaderContent { get; set; }

    public abstract bool Sortable { get; set; }
    public abstract DataSort<TItem>? SortBy { get; set; }

    protected internal abstract RenderFragment<TItem> CellContent { get; }

    public TableColumn()
    {
        HeaderContent = builder => builder.AddContent(0, Title);
    }

    protected override void OnInitialized()
    {
        Table.AddColumn(this);
    }
}
