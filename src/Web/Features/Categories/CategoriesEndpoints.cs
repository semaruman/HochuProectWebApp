using Microsoft.EntityFrameworkCore;
using Web.Common.Endpoints;
using Web.Infrastructure.Persistence;

namespace Web.Features.Categories;

public class CategoriesEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.Slug })
                .ToListAsync(ct);
            return Results.Ok(items);
        }).WithTags("Categories");
    }
}
