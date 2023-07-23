namespace Groceries.Transactions;

using Groceries.Common;
using Groceries.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

[Route("/transactions")]
public class TransactionsController : Controller
{
    private readonly AppDbContext dbContext;

    public TransactionsController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page, string? sort, string? dir)
    {
        var transactionsQuery = dbContext.Transactions
            .Join(
                dbContext.TransactionTotals,
                transaction => transaction.Id,
                transactionTotal => transactionTotal.TransactionId,
                (transaction, transactionTotal) => new
                {
                    transaction.Id,
                    transaction.CreatedAt,
                    Store = string.Concat(transaction.Store!.Retailer!.Name, " ", transaction.Store.Name),
                    TotalAmount = transactionTotal.Total,
                    TotalItems = transaction.Items.Sum(item => item.Quantity),
                });

        transactionsQuery = sort?.ToLowerInvariant() switch
        {
            "date" when dir == "desc" => transactionsQuery.OrderByDescending(transaction => transaction.CreatedAt),
            "amount" when dir == "desc" => transactionsQuery.OrderByDescending(transaction => transaction.TotalAmount),
            "items" when dir == "desc" => transactionsQuery.OrderByDescending(transaction => transaction.TotalItems),
            "date" => transactionsQuery.OrderBy(transaction => transaction.CreatedAt),
            "amount" => transactionsQuery.OrderBy(transaction => transaction.TotalAmount),
            "items" => transactionsQuery.OrderBy(transaction => transaction.TotalItems),
            _ => transactionsQuery.OrderByDescending(transaction => transaction.CreatedAt),
        };

        var transactions = await transactionsQuery
            .Select(transaction => new TransactionListModel.Transaction(transaction.Id, transaction.CreatedAt, transaction.Store)
            {
                TotalAmount = transaction.TotalAmount,
                TotalItems = transaction.TotalItems,
            })
            .ToListPageModelAsync(page);

        if (transactions.Page != page)
        {
            return RedirectToAction(nameof(Index), new { page = transactions.Page });
        }

        var model = new TransactionListModel(sort, dir, transactions);
        return View(model);
    }

    [HttpGet("new")]
    public IActionResult NewTransaction()
    {
        return View();
    }

    [HttpPost("new")]
    public IActionResult NewTransaction(DateTime createdAt, Guid storeId)
    {
        var transaction = new Transaction(createdAt.ToUniversalTime(), storeId);
        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return RedirectToAction(nameof(NewTransactionItems));
    }

    [HttpGet("new/items")]
    public IActionResult NewTransactionItems()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        return View(transaction);
    }

    [HttpPost("new/items")]
    public async Task<IActionResult> PostNewTransactionItems()
    {
        if (TempData["NewTransaction"] is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { page = 1 });
    }

    [HttpGet("new/items/new")]
    public IActionResult NewTransactionItem()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        return Request.IsTurboFrameRequest("modal")
            ? View($"{nameof(NewTransactionItem)}_Modal", transaction)
            : View(transaction);
    }

    [HttpPost("new/items/new")]
    public async Task<IActionResult> NewTransactionItem(string brand, string name, decimal price, int quantity)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var itemId = await dbContext.Items
            .Where(item => EF.Functions.ILike(item.Brand, brand) && EF.Functions.ILike(item.Name, name))
            .Select(item => item.Id)
            .SingleOrDefaultAsync();

        if (itemId == default)
        {
            var item = new Item(brand, name);
            dbContext.Items.Add(item);
            await dbContext.SaveChangesAsync();
            itemId = item.Id;
        }

        // TODO: Handle item already in transaction - merge, replace, error?

        var transactionItem = new TransactionItem(itemId, price, quantity);
        transaction.Items.Add(transactionItem);

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(NewTransactionItems));
    }

    [HttpGet("new/items/edit/{id}")]
    public IActionResult EditTransactionItem(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var transactionItem = transaction.Items.SingleOrDefault(item => item.ItemId == id);
        if (transactionItem == null)
        {
            return RedirectToAction(nameof(NewTransactionItems));
        }

        var model = (transaction, transactionItem);
        return Request.IsTurboFrameRequest("modal")
            ? View($"{nameof(EditTransactionItem)}_Modal", model)
            : View(model);
    }

    [HttpPost("new/items/edit/{id}")]
    public async Task<IActionResult> EditTransactionItem(Guid id, string brand, string name, decimal price, int quantity)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var transactionItem = transaction.Items.SingleOrDefault(item => item.ItemId == id);
        if (transactionItem == null)
        {
            return RedirectToAction(nameof(NewTransactionItems));
        }

        var itemId = await dbContext.Items
            .Where(item => EF.Functions.ILike(item.Brand, brand) && EF.Functions.ILike(item.Name, name))
            .Select(item => item.Id)
            .SingleOrDefaultAsync();

        if (itemId == transactionItem.ItemId)
        {
            transactionItem.Price = price;
            transactionItem.Quantity = quantity;
        }
        else
        {
            if (itemId == default)
            {
                var item = new Item(brand, name);
                dbContext.Items.Add(item);
                await dbContext.SaveChangesAsync();
                itemId = item.Id;
            }

            transaction.Items.Remove(transactionItem);

            transactionItem = new TransactionItem(itemId, price, quantity);
            transaction.Items.Add(transactionItem);
        }

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(NewTransactionItems));
    }

    [HttpPost("new/items/delete/{id}")]
    public IActionResult DeleteTransactionItem(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var transactionItem = transaction.Items.SingleOrDefault(item => item.ItemId == id);
        if (transactionItem != null)
        {
            transaction.Items.Remove(transactionItem);
        }

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(NewTransactionItems));
    }
}
