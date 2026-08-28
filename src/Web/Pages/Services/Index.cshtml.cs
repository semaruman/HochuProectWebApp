using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Enums;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Services;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<ServiceVm> Services { get; set; } = [];
    public record ServiceVm(Guid Id, string Title, decimal Price, int DeliveryDays, string CategoryName);

    public async Task OnGetAsync()
    {
        Services = await db.Services.AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.Status == ServiceStatus.Published)
            .OrderByDescending(s => s.CreatedAt)
            .Take(50)
            .Select(s => new ServiceVm(s.Id, s.Title, s.Price, s.DeliveryDays, s.Category.Name))
            .ToListAsync();
    }
}
