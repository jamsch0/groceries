namespace Groceries.Data;

public class ItemTagQuantity
{
    public required string Tag { get; init; }
    public required decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public bool IsMetric { get; init; }
    public bool IsDivisible { get; init; }
}
