namespace Web.Domain.Entities;

public class Message
{
    private Message()
    {
    }

    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public Conversation Conversation { get; private set; } = null!;
    public ApplicationUser Sender { get; private set; } = null!;

    public static Message Create(Guid conversationId, Guid senderId, string text, DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId,
        SenderId = senderId,
        Text = text.Trim(),
        CreatedAt = utcNow
    };

    public void MarkRead(DateTime utcNow) => ReadAt ??= utcNow;
}
