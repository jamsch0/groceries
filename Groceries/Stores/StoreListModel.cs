namespace Groceries.Stores;

public record StoreListModel(string? Search, ListPageModel<StoreListModel.Store> Stores)
{
    public record Store(Guid Id, string Retailer, string Name)
    {
        public int TotalTransactions { get; init; }
    }
}
