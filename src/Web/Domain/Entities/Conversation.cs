namespace Web.Domain.Entities;

public class Conversation
{
    private Conversation()
    {
    }

    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Deal Deal { get; private set; } = null!;
    public ICollection<Message> Messages { get; private set; } = new List<Message>();

    public static Conversation Open(Guid dealId, DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        DealId = dealId,
        CreatedAt = utcNow
    };
}
