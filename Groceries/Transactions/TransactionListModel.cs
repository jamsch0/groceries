namespace Groceries.Transactions;

public record TransactionListModel(string? Sort, string? Dir, ListPageModel<TransactionListModel.Transaction> Transactions)
{
    public record Transaction(Guid Id, DateTime CreatedAt, string Store)
    {
        public decimal TotalAmount { get; init; }
        public int TotalItems { get; init; }
    }
}
