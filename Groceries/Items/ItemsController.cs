namespace Groceries.Items;

using Groceries.Common;
using Groceries.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/items")]
public class ItemsController : Controller
{
    private readonly AppDbContext dbContext;

    public ItemsController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page, string? search)
    {
        var itemsQuery = dbContext.Items.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            var searchPattern = $"%{search}%";
            itemsQuery = itemsQuery.Where(item => EF.Functions.ILike(item.Brand + ' '  + item.Name, searchPattern));
        }

        var lastPurchasesQuery = dbContext.ItemPurchases
            .GroupBy(purchase => purchase.ItemId)
            .Select(purchases => new
            {
                ItemId = purchases.Key,
                CreatedAt = purchases.Max(purchase => purchase.CreatedAt),
            });

        var items = await itemsQuery
            .OrderBy(item => item.Brand)
            .ThenBy(item => item.Name)
            .GroupJoin(
                lastPurchasesQuery,
                item => item.Id,
                lastPurchase => lastPurchase.ItemId,
                (item, lastPurchase) => new { item, lastPurchase })
            .SelectMany(
                group => group.lastPurchase.DefaultIfEmpty(),
                (group, lastPurchase) => new ItemListModel.Item(group.item.Id, group.item.Brand, group.item.Name)
                {
                    HasBarcode = group.item.Barcodes.Any(),
                    LastPurchasedAt = lastPurchase != null ? lastPurchase.CreatedAt : null,
                })
            .ToListPageModelAsync(page, cancellationToken: HttpContext.RequestAborted);

        if (items.Page != page)
        {
            return RedirectToAction(nameof(Index), new { page = items.Page, search });
        }

        var model = new ItemListModel(search, items);
        return View(model);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> EditItem(Guid id)
    {
        var item = await dbContext.Items
            .SingleOrDefaultAsync(item => item.Id == id, HttpContext.RequestAborted);

        if (item == null)
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> EditItem(Guid id, string brand, string name)
    {
        var item = await dbContext.Items
            .SingleOrDefaultAsync(item => item.Id == id, HttpContext.RequestAborted);

        if (item == null)
        {
            return NotFound();
        }

        if (Request.AcceptsTurboStream())
        {
            return this.TurboStream(TurboStreamAction.Replace, "modal-body", item);
        }

        return RedirectToAction(nameof(EditItem));
    }
}
