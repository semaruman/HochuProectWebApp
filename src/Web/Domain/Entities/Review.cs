using Web.Domain.Exceptions;

namespace Web.Domain.Entities;

public class Review
{
    private Review()
    {
    }

    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid RecipientId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public Deal Deal { get; private set; } = null!;
    public ApplicationUser Author { get; private set; } = null!;
    public ApplicationUser Recipient { get; private set; } = null!;

    public static Review Create(
        Guid dealId,
        Guid authorId,
        Guid recipientId,
        int rating,
        string comment,
        DateTime utcNow)
    {
        if (authorId == recipientId)
            throw new DomainException("Cannot review yourself.");
        if (rating is < 1 or > 5)
            throw new DomainException("Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 5)
            throw new DomainException("Comment is too short.");

        return new Review
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            AuthorId = authorId,
            RecipientId = recipientId,
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = utcNow
        };
    }
}
