using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Common.Auth;
using Web.Common.Errors;
using Web.Domain.Enums;
using Web.Features.Bids;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Projects;

public class DetailsModel(AppDbContext db, ICurrentUser currentUser, CreateBidHandler createBid) : PageModel
{
    public ProjectVm? Project { get; set; }
    public bool IsOwner { get; set; }
    public bool CanBid { get; set; }
    public string? Error { get; set; }

    public record ProjectVm(Guid Id, string Title, string Description, decimal BudgetAmount, DateOnly Deadline, ProjectStatus Status, string CategoryName, Guid BuyerId);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var project = await db.Projects.AsNoTracking().Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        var userId = currentUser.TryGetUserId();
        if (project.Status == ProjectStatus.Draft && project.BuyerId != userId)
            return Forbid();

        Project = new ProjectVm(project.Id, project.Title, project.Description, project.BudgetAmount, project.Deadline, project.Status, project.Category.Name, project.BuyerId);
        IsOwner = userId == project.BuyerId;
        CanBid = userId is not null && !IsOwner && project.Status == ProjectStatus.Published;
        return Page();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        var userId = currentUser.UserId;
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        if (!project.IsOwner(userId)) return Forbid();
        return await PageCommand.ExecuteAsync(
            async () =>
            {
                project.Publish(DateTime.UtcNow);
                await db.SaveChangesAsync();
                return RedirectToPage(new { id });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });
    }

    public Task<IActionResult> OnPostBidAsync(Guid id, decimal price, int estimatedDays, string coverLetter) =>
        PageCommand.ExecuteAsync(
            async () =>
            {
                await createBid.HandleAsync(id, currentUser.UserId, price, estimatedDays, coverLetter ?? string.Empty, HttpContext.RequestAborted);
                return RedirectToPage(new { id });
            },
            async error =>
            {
                Error = error;
                await OnGetAsync(id);
                return Page();
            });
}
