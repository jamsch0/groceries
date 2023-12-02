namespace Groceries.Transactions;

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
    public IActionResult PostNewTransactionItems()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        return RedirectToAction(nameof(NewTransactionPromotions));
    }

    [HttpGet("new/items/new")]
    public async Task<IActionResult> NewTransactionItem(long? barcodeData, string? barcodeFormat)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        TransactionItem? transactionItem = null;
        if (barcodeData != null && barcodeFormat != null)
        {
            var item = await dbContext.Items
                .Where(item => item.Barcodes.Any(barcode => barcode.BarcodeData == barcodeData))
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefaultAsync();

            item ??= new Item(id: default);
            item.Barcodes.Add(new ItemBarcode(item.Id, barcodeData.Value, barcodeFormat));

            // TODO: Fix `MinValue` hack - view models?
            transactionItem = new TransactionItem(item.Id, decimal.MinValue, int.MinValue) { Item = item };
        }

        var model = (transaction, transactionItem);
        return Request.IsTurboFrameRequest("modal")
            ? View($"{nameof(NewTransactionItem)}_Modal", model)
            : View(model);
    }

    [HttpPost("new/items/new")]
    public async Task<IActionResult> NewTransactionItem(string brand, string name, decimal price, int quantity, long? barcodeData, string? barcodeFormat)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var itemId = await dbContext.Items
            .Where(item => EF.Functions.ILike(item.Brand, brand) && EF.Functions.ILike(item.Name, name))
            .Select(item => item.Id)
            .SingleOrDefaultAsync();

        var item = new Item(itemId, brand, name);
        if (barcodeData != null && barcodeFormat != null)
        {
            var barcode = new ItemBarcode(itemId, barcodeData.Value, barcodeFormat);
            item.Barcodes.Add(barcode);

            if (!await dbContext.ItemBarcodes.ContainsAsync(barcode))
            {
                dbContext.ItemBarcodes.Add(barcode);
            }
        }

        dbContext.Items.Attach(item);
        await dbContext.SaveChangesAsync();

        // TODO: Handle item already in transaction - merge, replace, error?

        var transactionItem = new TransactionItem(item.Id, price, quantity) { Item = item };
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

        var item = new Item(itemId, brand, name);

        dbContext.Items.Attach(item);
        await dbContext.SaveChangesAsync();

        transactionItem.Item = item;
        transactionItem.ItemId = item.Id;
        transactionItem.Price = price;
        transactionItem.Quantity = quantity;

        // TODO: Handle barcode when editing item - replace, disable?

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

    [HttpGet("new/promotions")]
    public IActionResult NewTransactionPromotions()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        return View(transaction);
    }

    [HttpPost("new/promotions")]
    public async Task<IActionResult> PostNewTransactionPromotions()
    {
        if (TempData["NewTransaction"] is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        // Work around EF trying to insert items by explicitly tracking them as unchanged
        dbContext.Items.AttachRange(
            transaction.Items
                .Select(item => item.Item!)
                .Concat(transaction.Promotions.SelectMany(promotion => promotion.Items)));

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { page = 1 });
    }

    [HttpGet("new/promotions/new")]
    public IActionResult NewTransactionPromotion()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        return Request.IsTurboFrameRequest("modal")
            ? View($"{nameof(NewTransactionPromotion)}_Modal", transaction)
            : View(transaction);
    }

    [HttpPost("new/promotions/new")]
    public IActionResult NewTransactionPromotion(string name, decimal amount, Guid[] itemIds)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        // TODO: Handle promotion already in transaction - merge, replace, error?

        var promotion = new TransactionPromotion(name, amount) { Items = itemIds.Select(id => new Item(id)).ToArray() };
        transaction.Promotions.Add(promotion);

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(NewTransactionPromotions));
    }

    [HttpGet("new/promotions/edit/{id}")]
    public IActionResult EditTransactionPromotion(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var promotion = transaction.Promotions.SingleOrDefault(promotion => promotion.Id == id);
        if (promotion == null)
        {
            return RedirectToAction(nameof(NewTransactionPromotions));
        }

        var model = (transaction, promotion);
        return Request.IsTurboFrameRequest("modal")
            ? View($"{nameof(EditTransactionPromotion)}_Modal", model)
            : View(model);
    }

    [HttpPost("new/promotions/edit/{id}")]
    public IActionResult EditTransactionPromotion(Guid id, string name, decimal amount, Guid[] itemIds)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var promotion = transaction.Promotions.SingleOrDefault(promotion => promotion.Id == id);
        if (promotion == null)
        {
            return RedirectToAction(nameof(NewTransactionPromotions));
        }

        promotion.Name = name;
        promotion.Amount = amount;
        promotion.Items = itemIds.Select(id => new Item(id)).ToArray();

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(NewTransactionPromotions));
    }

    [HttpPost("new/promotions/delete/{id}")]
    public IActionResult DeleteTransactionPromotion(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return RedirectToAction(nameof(NewTransaction));
        }

        var promotion = transaction.Promotions.SingleOrDefault(promotion => promotion.Id == id);
        if (promotion != null)
        {
            transaction.Promotions.Remove(promotion);
        }

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(NewTransactionPromotions));
    }
}
