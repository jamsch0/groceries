namespace Groceries.Items;

public record ItemListModel(string? Search, ListPageModel<ItemListModel.Item> Items)
{
    public record Item(Guid Id, string Brand, string Name)
    {
        public bool HasBarcode { get; init; }
        public DateTime? LastPurchasedAt { get; init; }
    }
}
