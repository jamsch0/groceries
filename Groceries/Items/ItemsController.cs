namespace Groceries.Items;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("/items")]
public class ItemsController : ControllerBase
{
    [HttpGet]
    public IResult Index()
    {
        return new RazorComponentResult<ItemsPage>();
    }
}
