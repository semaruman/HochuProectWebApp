namespace Web.Domain.Entities;

public class DealDeliverable
{
    private DealDeliverable()
    {
    }

    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public string? Message { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Deal Deal { get; private set; } = null!;
    public ICollection<DealDeliverableFile> Files { get; private set; } = new List<DealDeliverableFile>();

    public static DealDeliverable Create(Guid dealId, string? message, DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        DealId = dealId,
        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
        CreatedAt = utcNow
    };

    public DealDeliverableFile AddFile(string storageKey, string fileName, string contentType, long sizeBytes)
    {
        var file = DealDeliverableFile.Create(Id, storageKey, fileName, contentType, sizeBytes);
        Files.Add(file);
        return file;
    }
}
