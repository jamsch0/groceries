namespace Groceries.Stores;

using Groceries.Common;
using Groceries.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/stores")]
public class StoresController : Controller
{
    private readonly AppDbContext dbContext;

    public StoresController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page, string? search)
    {
        var storesQuery = dbContext.Stores.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            var searchPattern = $"%{search}%";
            storesQuery = storesQuery.Where(store => EF.Functions.ILike(store.Retailer!.Name + ' ' + store.Name, searchPattern));
        }

        var stores = await storesQuery
            .OrderBy(store => store.Retailer!.Name)
            .ThenBy(store => store.Name)
            .Select(store => new StoreListModel.Store(store.Id, store.Retailer!.Name, store.Name)
            {
                TotalTransactions = store.Transactions!.Count(),
            })
            .ToListPageModelAsync(page, cancellationToken: HttpContext.RequestAborted);

        if (stores.Page != page)
        {
            return RedirectToAction(nameof(Index), new { page = stores.Page, search });
        }

        var model = new StoreListModel(search, stores);
        return View(model);
    }

    [HttpGet("new")]
    public IActionResult NewStore()
    {
        return Request.IsTurboFrameRequest("modal")
            ? View($"{nameof(NewStore)}_Modal")
            : View();
    }

    [HttpPost("new")]
    public async Task<IActionResult> NewStore(Guid retailerId, string name, string? address)
    {
        var store = new Store(retailerId, name, address);
        dbContext.Stores.Add(store);

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(Index), new { page = 1 });
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> EditStore(Guid id)
    {
        var store = await dbContext.Stores
            .SingleOrDefaultAsync(store => store.Id == id, HttpContext.RequestAborted);

        if (store == null)
        {
            return NotFound();
        }

        return Request.IsTurboFrameRequest("modal")
            ? View($"{nameof(EditStore)}_Modal", store)
            : View(store);
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> EditStore(Guid id, Guid retailerId, string name, string? address)
    {
        var store = new Store(id, retailerId, name, address);
        dbContext.Stores.Update(store);

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Request.IsTurboFrameRequest("modal")
            ? NoContent()
            : RedirectToAction(nameof(EditStore));
    }
}
