namespace Groceries.Transactions;

using Groceries.Data;
using Microsoft.AspNetCore.Http.HttpResults;
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
    public IResult Index()
    {
        return new RazorComponentResult<TransactionsPage>();
    }

    [HttpGet("new")]
    public IResult NewTransaction()
    {
        return new RazorComponentResult<NewTransactionPage>();
    }

    [HttpPost("new")]
    public IResult NewTransaction(DateTime createdAt, Guid storeId)
    {
        var transaction = new Transaction(createdAt.ToUniversalTime(), storeId);
        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Results.LocalRedirect("/transactions/new/items");
    }

    [HttpGet("new/items")]
    public IResult NewTransactionItems()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        return new RazorComponentResult<NewTransactionItemsPage>(new { Transaction = transaction });
    }

    [HttpPost("new/items")]
    public IResult PostNewTransactionItems()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is null)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        return Results.LocalRedirect("/transactions/new/promotions");
    }

    [HttpGet("new/items/new")]
    public async Task<IResult> NewTransactionItem(long? barcodeData, string? barcodeFormat)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        TransactionItem? transactionItem = null;
        if (barcodeData != null && barcodeFormat != null)
        {
            var item = await dbContext.Items
                .Where(item => item.Barcodes.Any(barcode => barcode.BarcodeData == barcodeData))
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefaultAsync();

            item ??= new Item(id: default);

            var barcode = new ItemBarcode(item.Id, barcodeData.Value, barcodeFormat);
            item.Barcodes.Add(barcode);

            if (item.Id != default)
            {
                barcode.LastScannedAt = DateTime.UtcNow;
                dbContext.Update(barcode);
                await dbContext.SaveChangesAsync();
            }

            // TODO: Fix `MinValue` hack - view models?
            transactionItem = new TransactionItem(item.Id, decimal.MinValue, int.MinValue) { Item = item };
        }

        var parameters = new { Transaction = transaction, TransactionItem = transactionItem };
        return Request.IsTurboFrameRequest("modal")
            ? new RazorComponentResult<NewTransactionItemModal>(parameters)
            : new RazorComponentResult<NewTransactionItemPage>(parameters);
    }

    [HttpPost("new/items/new")]
    public async Task<IResult> NewTransactionItem(string brand, string name, decimal price, int quantity, long? barcodeData, string? barcodeFormat)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
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
            ? Results.NoContent()
            : Results.LocalRedirect("/transactions/new/items");
    }

    [HttpGet("new/items/edit/{id}")]
    public IResult EditTransactionItem(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        var transactionItem = transaction.Items.SingleOrDefault(item => item.ItemId == id);
        if (transactionItem == null)
        {
            return Results.LocalRedirect("/transactions/new/items");
        }

        var parameters = new { Transaction = transaction, TransactionItem = transactionItem };
        return Request.IsTurboFrameRequest("modal")
            ? new RazorComponentResult<EditTransactionItemModal>(parameters)
            : new RazorComponentResult<EditTransactionItemPage>(parameters);
    }

    [HttpPost("new/items/edit/{id}")]
    public async Task<IResult> EditTransactionItem(Guid id, string brand, string name, decimal price, int quantity)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        var transactionItem = transaction.Items.SingleOrDefault(item => item.ItemId == id);
        if (transactionItem == null)
        {
            return Results.LocalRedirect("/transactions/new/items");
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
            ? Results.NoContent()
            : Results.LocalRedirect("/transactions/new/items");
    }

    [HttpPost("new/items/delete/{id}")]
    public IResult DeleteTransactionItem(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        var transactionItem = transaction.Items.SingleOrDefault(item => item.ItemId == id);
        if (transactionItem != null)
        {
            transaction.Items.Remove(transactionItem);
        }

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? Results.NoContent()
            : Results.LocalRedirect("/transactions/new/items");
    }

    [HttpGet("new/promotions")]
    public IResult NewTransactionPromotions()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        return new RazorComponentResult<NewTransactionPromotionsPage>(new { Transaction = transaction });
    }

    [HttpPost("new/promotions")]
    public async Task<IResult> PostNewTransactionPromotions()
    {
        if (TempData["NewTransaction"] is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        // Work around EF trying to insert items by explicitly tracking them as unchanged
        dbContext.Items.AttachRange(
            transaction.Items
                .Select(item => item.Item!)
                .Concat(transaction.Promotions.SelectMany(promotion => promotion.Items)));

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return Results.LocalRedirect("/transactions?page=1");
    }

    [HttpGet("new/promotions/new")]
    public IResult NewTransactionPromotion()
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        var parameters = new { Transaction = transaction };
        return Request.IsTurboFrameRequest("modal")
            ? new RazorComponentResult<NewTransactionPromotionModal>(parameters)
            : new RazorComponentResult<NewTransactionPromotionPage>(parameters);
    }

    [HttpPost("new/promotions/new")]
    public IResult NewTransactionPromotion(string name, decimal amount, Guid[] itemIds)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        // TODO: Handle promotion already in transaction - merge, replace, error?

        var promotion = new TransactionPromotion(name, amount) { Items = itemIds.Select(id => new Item(id)).ToArray() };
        transaction.Promotions.Add(promotion);

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? Results.NoContent()
            : Results.LocalRedirect("/transactions/new/promotions");
    }

    [HttpGet("new/promotions/edit/{id}")]
    public IResult EditTransactionPromotion(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        var promotion = transaction.Promotions.SingleOrDefault(promotion => promotion.Id == id);
        if (promotion == null)
        {
            return Results.LocalRedirect("/transactions/new/promotions");
        }

        var parameters = new { Transaction = transaction, Promotion = promotion };
        return Request.IsTurboFrameRequest("modal")
            ? new RazorComponentResult<EditTransactionPromotionModal>(parameters)
            : new RazorComponentResult<EditTransactionPromotionPage>(parameters);
    }

    [HttpPost("new/promotions/edit/{id}")]
    public IResult EditTransactionPromotion(Guid id, string name, decimal amount, Guid[] itemIds)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        var promotion = transaction.Promotions.SingleOrDefault(promotion => promotion.Id == id);
        if (promotion == null)
        {
            return Results.LocalRedirect("/transactions/new/promotions");
        }

        promotion.Name = name;
        promotion.Amount = amount;
        promotion.Items = itemIds.Select(id => new Item(id)).ToArray();

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? Results.NoContent()
            : Results.LocalRedirect("/transactions/new/promotions");
    }

    [HttpPost("new/promotions/delete/{id}")]
    public IResult DeleteTransactionPromotion(Guid id)
    {
        if (TempData.Peek("NewTransaction") is not string json || JsonSerializer.Deserialize<Transaction>(json) is not Transaction transaction)
        {
            return Results.LocalRedirect("/transactions/new");
        }

        var promotion = transaction.Promotions.SingleOrDefault(promotion => promotion.Id == id);
        if (promotion != null)
        {
            transaction.Promotions.Remove(promotion);
        }

        TempData["NewTransaction"] = JsonSerializer.Serialize(transaction);

        return Request.IsTurboFrameRequest("modal")
            ? Results.NoContent()
            : Results.LocalRedirect("/transactions/new/promotions");
    }
}
