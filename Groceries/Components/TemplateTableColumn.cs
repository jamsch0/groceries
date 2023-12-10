namespace Groceries.Components;

using Microsoft.AspNetCore.Components;

public class TemplateTableColumn<TItem> : TableColumn<TItem>
{
    [Parameter]
    public RenderFragment<TItem> ChildContent { get; set; } = _ => _ => { };

    [Parameter]
    public override DataSort<TItem>? SortBy { get; set; }

    public override bool Sortable
    {
        get => SortBy != null;
        set => throw new NotSupportedException();
    }

    protected internal override RenderFragment<TItem> CellContent => ChildContent;
}
