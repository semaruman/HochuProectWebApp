using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Enums;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Services;

public class DetailsModel(AppDbContext db) : PageModel
{
    public ServiceVm? Service { get; set; }
    public record ServiceVm(Guid Id, string Title, string Description, decimal Price, int DeliveryDays, string CategoryName, Guid SellerId);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var s = await db.Services.AsNoTracking().Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == ServiceStatus.Published);
        if (s is null) return NotFound();
        Service = new ServiceVm(s.Id, s.Title, s.Description, s.Price, s.DeliveryDays, s.Category.Name, s.SellerId);
        return Page();
    }
}
