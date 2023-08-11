namespace Groceries.Home;

using Groceries.Data;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("/")]
public class HomeController : Controller
{
    private readonly AppDbContext dbContext;

    public HomeController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAsync()
    {
        var randomTagQuantity = await dbContext.ItemTagQuantities
            .FromSql($"""
                SELECT tag, quantity, coalesce(unit_name, unit) AS unit, is_metric, is_divisible
                FROM (
                    SELECT
                        unnest(tags) AS tag,
                        round(sum((item_quantity->'amount')::numeric * quantity), 1) AS quantity,
                        item_quantity->>'unit' AS unit,
                        (item_quantity->'is_metric')::boolean AS is_metric,
                        (item_quantity->'is_divisible')::boolean AS is_divisible
                    FROM item_purchases
                    JOIN items USING (item_id)
                    CROSS JOIN item_quantity(name)
                    WHERE array_length(tags, 1) > 0
                        AND age(created_at) <= '90 days'
                        AND item_quantity IS NOT NULL
                    GROUP BY tag, item_quantity->>'unit', item_quantity->'is_metric', item_quantity->'is_divisible'
                    ORDER BY random()
                    FETCH FIRST ROW ONLY
                ) AS random_item_tag_quantity
                LEFT JOIN item_tags USING (tag)
                """)
            .FirstOrDefaultAsync();

        return View(randomTagQuantity);
    }
}
