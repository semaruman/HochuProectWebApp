using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Errors;
using Web.Domain.Enums;
using Web.Features.Deals;
using Web.Features.Reviews;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Deals;

[Authorize]
public class DetailsModel(
    AppDbContext db,
    ICurrentUser currentUser,
    FundDealHandler fundDeal,
    SubmitWorkHandler submitWork,
    AcceptWorkHandler acceptWork,
    CancelDealHandler cancelDeal,
    CreateReviewHandler createReview) : PageModel
{
    public DealVm? Deal { get; set; }
    public bool IsBuyer { get; set; }
    public bool IsSeller { get; set; }
    public Guid CurrentUserId { get; set; }
    public string? Error { get; set; }

    public record DealVm(Guid Id, string ProjectTitle, decimal Amount, DealStatus Status, Guid BuyerId, Guid SellerId, Guid ProjectId);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        CurrentUserId = currentUser.UserId;
        var deal = await db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (deal is null) return NotFound();
        if (!deal.IsParticipant(CurrentUserId)) return Forbid();

        var title = await db.Projects.Where(p => p.Id == deal.ProjectId).Select(p => p.Title).FirstAsync();
        Deal = new DealVm(deal.Id, title, deal.Amount, deal.Status, deal.BuyerId, deal.SellerId, deal.ProjectId);
        IsBuyer = deal.BuyerId == CurrentUserId;
        IsSeller = deal.SellerId == CurrentUserId;
        return Page();
    }

    public Task<IActionResult> OnPostFundAsync(Guid id) =>
        PageCommand.ExecuteAsync(
            async () =>
            {
                await fundDeal.HandleAsync(id, currentUser.UserId, HttpContext.RequestAborted);
                return RedirectToPage(new { id });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });

    public Task<IActionResult> OnPostSubmitAsync(Guid id, string? message) =>
        PageCommand.ExecuteAsync(
            async () =>
            {
                await submitWork.HandleAsync(id, currentUser.UserId, message, null, HttpContext.RequestAborted);
                return RedirectToPage(new { id });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });

    public Task<IActionResult> OnPostAcceptAsync(Guid id) =>
        PageCommand.ExecuteAsync(
            async () =>
            {
                await acceptWork.HandleAsync(id, currentUser.UserId, HttpContext.RequestAborted);
                return RedirectToPage(new { id });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });

    public Task<IActionResult> OnPostCancelAsync(Guid id) =>
        PageCommand.ExecuteAsync(
            async () =>
            {
                await cancelDeal.HandleAsync(id, currentUser.UserId, HttpContext.RequestAborted);
                return RedirectToPage(new { id });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });

    public Task<IActionResult> OnPostReviewAsync(Guid id, int rating, string comment) =>
        PageCommand.ExecuteAsync(
            async () =>
            {
                await createReview.HandleAsync(id, currentUser.UserId, rating, comment, HttpContext.RequestAborted);
                return RedirectToPage(new { id });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });
}
