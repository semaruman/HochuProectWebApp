using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Notifications;

[Authorize]
public class IndexModel(AppDbContext db) : PageModel
{
    public List<ItemVm> Items { get; set; } = [];
    public record ItemVm(Guid Id, string Title, string Body, string? LinkUrl, bool IsRead, DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Items = await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new ItemVm(n.Id, n.Title, n.Body, n.LinkUrl, n.IsRead, n.CreatedAt))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostReadAllAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return RedirectToPage();
    }
}
