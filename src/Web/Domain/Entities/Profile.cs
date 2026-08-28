using Web.Common.Results;

namespace Web.Domain.Entities;

public class Profile
{
    private Profile()
    {
    }

    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarPath { get; private set; }
    public string? Bio { get; private set; }
    public decimal? AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ApplicationUser User { get; private set; } = null!;
    public ICollection<UserSkill> UserSkills { get; private set; } = new List<UserSkill>();
    public ICollection<PortfolioItem> PortfolioItems { get; private set; } = new List<PortfolioItem>();

    public static Result<Profile> Create(Guid userId, string displayName, DateTime utcNow, string? bio = null)
    {
        if (userId == Guid.Empty)
            return ResultErrors.Business("User is required.");
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length < 2)
            return ResultErrors.Business("Display name is too short.");

        return new Profile
        {
            UserId = userId,
            DisplayName = displayName.Trim(),
            Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public Result Update(string displayName, string? bio, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length < 2)
            return ResultErrors.Business("Display name is too short.");
        DisplayName = displayName.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        UpdatedAt = utcNow;
        return Result.Success();
    }

    public void SetAvatar(string storageKey, DateTime utcNow)
    {
        AvatarPath = storageKey;
        UpdatedAt = utcNow;
    }

    public void RecalculateRating(IReadOnlyCollection<int> ratings)
    {
        if (ratings.Count == 0)
        {
            ReviewCount = 0;
            AverageRating = null;
            return;
        }

        ReviewCount = ratings.Count;
        AverageRating = Math.Round((decimal)ratings.Average(), 2, MidpointRounding.AwayFromZero);
    }
}
