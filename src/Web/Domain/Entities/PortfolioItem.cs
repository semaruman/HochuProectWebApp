namespace Web.Domain.Entities;

public class PortfolioItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? FilePath { get; set; }
    public DateTime CreatedAt { get; set; }

    public Profile Profile { get; set; } = null!;
}
