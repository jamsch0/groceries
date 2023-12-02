namespace Groceries.Home;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IResult Index()
    {
        return new RazorComponentResult<HomePage>();
    }
}
