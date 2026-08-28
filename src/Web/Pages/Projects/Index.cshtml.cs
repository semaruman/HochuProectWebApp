using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Enums;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Projects;

public class IndexModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];
    public List<ProjectListItem> Projects { get; set; } = [];

    public record ProjectListItem(Guid Id, string Title, decimal BudgetAmount, DateOnly Deadline, string CategoryName);

    public async Task OnGetAsync()
    {
        CategoryOptions = await db.Categories.OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(), CategoryId == c.Id))
            .ToListAsync();

        var query = db.Projects.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == ProjectStatus.Published);

        if (CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == CategoryId);
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Title, $"%{term}%") || EF.Functions.ILike(p.Description, $"%{term}%"));
        }

        Projects = await query.OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .Select(p => new ProjectListItem(p.Id, p.Title, p.BudgetAmount, p.Deadline, p.Category.Name))
            .ToListAsync();
    }
}
