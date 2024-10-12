namespace Groceries.Stores;

using Groceries.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/stores")]
public class StoresController : Controller
{
    private readonly IDbContextFactory<AppDbContext> dbContextFactory;

    public StoresController(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    public IResult Index()
    {
        return new RazorComponentResult<StoresPage>();
    }

    [HttpGet("new")]
    public IResult NewStore()
    {
        return Request.IsTurboFrameRequest("modal")
            ? new RazorComponentResult<NewStoreModal>()
            : new RazorComponentResult<NewStorePage>();
    }

    [HttpPost("new")]
    public async Task<IResult> NewStore(Guid retailerId, string name, string? address)
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        var store = new Store(retailerId, name, address);
        dbContext.Stores.Add(store);

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Request.IsTurboFrameRequest("modal")
            ? Results.NoContent()
            : Results.LocalRedirect("/stores?page=1");
    }

    [HttpGet("edit/{id}")]
    public async Task<IResult> EditStore(Guid id)
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        var store = await dbContext.Stores
            .SingleOrDefaultAsync(store => store.Id == id, HttpContext.RequestAborted);

        if (store == null)
        {
            return Results.NotFound();
        }

        return Request.IsTurboFrameRequest("modal")
            ? new RazorComponentResult<EditStoreModal>(new { Store = store })
            : new RazorComponentResult<EditStorePage>(new { Store = store });
    }

    [HttpPost("edit/{id}")]
    public async Task<IResult> EditStore(Guid id, Guid retailerId, string name, string? address, string? returnUrl)
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        var store = new Store(id, retailerId, name, address);
        dbContext.Stores.Update(store);

        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);

        return Request.IsTurboFrameRequest("modal")
            ? Results.NoContent()
            : Results.LocalRedirect($"/stores/edit/{id}");
    }
}
