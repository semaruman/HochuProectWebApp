using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Errors;
using Web.Domain.Enums;
using Web.Features.Bids;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Projects;

[Authorize]
public class BidsModel(AppDbContext db, ICurrentUser currentUser, AcceptBidHandler acceptBid) : PageModel
{
    public string ProjectTitle { get; set; } = string.Empty;
    public bool CanAccept { get; set; }
    public string? Error { get; set; }
    public List<BidVm> Bids { get; set; } = [];

    public record BidVm(Guid Id, Guid SellerId, string SellerName, decimal Price, int EstimatedDays, string CoverLetter, BidStatus Status, Guid? DealId);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userId = currentUser.UserId;
        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        if (project.BuyerId != userId) return Forbid();

        ProjectTitle = project.Title;
        CanAccept = project.Status == ProjectStatus.Published;

        Bids = await db.Bids.AsNoTracking()
            .Where(b => b.ProjectId == id)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BidVm(
                b.Id,
                b.SellerId,
                db.Profiles.Where(p => p.UserId == b.SellerId).Select(p => p.DisplayName).FirstOrDefault() ?? "Инженер",
                b.Price,
                b.EstimatedDays,
                b.CoverLetter,
                b.Status,
                db.Deals.Where(d => d.BidId == b.Id).Select(d => (Guid?)d.Id).FirstOrDefault()))
            .ToListAsync();

        return Page();
    }

    public Task<IActionResult> OnPostAsync(Guid id, Guid bidId) =>
        PageCommand.ExecuteAsync(
            async () =>
            {
                var result = await acceptBid.HandleAsync(bidId, currentUser.UserId, HttpContext.RequestAborted);
                return RedirectToPage("/Deals/Details", new { id = result.DealId });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });
}
