using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Web.Domain.Entities;
using Web.Infrastructure.Persistence;

namespace Web.Pages.Profiles;

public class DetailsModel(AppDbContext db) : PageModel
{
    public Profile? Profile { get; set; }
    public List<string> Skills { get; set; } = [];
    public List<ReviewVm> Reviews { get; set; } = [];
    public record ReviewVm(int Rating, string Comment, string AuthorName);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Profile = await db.Profiles.AsNoTracking()
            .Include(p => p.UserSkills).ThenInclude(us => us.Skill)
            .FirstOrDefaultAsync(p => p.UserId == id);
        if (Profile is null) return NotFound();
        Skills = Profile.UserSkills.Select(us => us.Skill.Name).ToList();
        Reviews = await db.Reviews.AsNoTracking()
            .Where(r => r.RecipientId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewVm(
                r.Rating,
                r.Comment,
                db.Profiles.Where(p => p.UserId == r.AuthorId).Select(p => p.DisplayName).FirstOrDefault() ?? "Пользователь"))
            .ToListAsync();
        return Page();
    }
}
