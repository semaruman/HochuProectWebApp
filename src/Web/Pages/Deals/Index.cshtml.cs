using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Enums;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Deals;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<DealVm> Deals { get; set; } = [];
    public record DealVm(Guid Id, string ProjectTitle, decimal Amount, DealStatus Status);

    public async Task OnGetAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Deals = await db.Deals.AsNoTracking()
            .Where(d => d.BuyerId == userId || d.SellerId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DealVm(
                d.Id,
                db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Title).FirstOrDefault() ?? "Проект",
                d.Amount,
                d.Status))
            .ToListAsync();
    }
}
