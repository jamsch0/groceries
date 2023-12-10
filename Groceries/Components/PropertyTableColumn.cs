namespace Groceries.Components;

using Humanizer;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

public class PropertyTableColumn<TItem, TProp> : TableColumn<TItem>
{
    private Expression<Func<TItem, TProp>>? lastAssignedProperty;
    private Func<TItem, TProp>? compiledPropertyExpression;
    private Func<TItem, string?>? cellTextFunc;
    private DataSort<TItem>? sortBy;

    [Parameter, EditorRequired]
    public required Expression<Func<TItem, TProp>> Property { get; set; }

    [Parameter]
    public RenderFragment<TProp>? ChildContent { get; set; }

    [Parameter]
    public string? Format { get; set; }

    [Parameter]
    public override bool Sortable { get; set; }

    public override DataSort<TItem>? SortBy
    {
        get => sortBy;
        set => throw new NotSupportedException();
    }

    protected internal override RenderFragment<TItem> CellContent
        => ChildContent != null
            ? item => ChildContent(compiledPropertyExpression!(item))
            : item => builder => builder.AddContent(0, cellTextFunc?.Invoke(item));

    protected override void OnParametersSet()
    {
        if (Title is null && Property.Body is MemberExpression memberExpression)
        {
            Title = memberExpression.Member.Name;
        }
        if (Align is null && (typeof(TProp) == typeof(int) || typeof(TProp) == typeof(decimal)))
        {
            Align = Components.Align.End;
        }

        if (lastAssignedProperty == Property)
        {
            return;
        }

        lastAssignedProperty = Property;
        compiledPropertyExpression = Property.Compile();

        if (ChildContent == null)
        {
            if (!string.IsNullOrEmpty(Format) &&
                typeof(IFormattable).IsAssignableFrom(Nullable.GetUnderlyingType(typeof(TProp)) ?? typeof(TProp)))
            {
                cellTextFunc = item => ((IFormattable?)compiledPropertyExpression(item))?.ToString(Format, null);
            }
            else
            {
                cellTextFunc = item => compiledPropertyExpression(item)?.ToString();
            }
        }

        sortBy = DataSort.By(Property, key: Title.Camelize());
    }
}
