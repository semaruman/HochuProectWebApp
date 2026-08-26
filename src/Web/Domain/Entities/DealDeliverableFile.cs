namespace Web.Domain.Entities;

public class DealDeliverableFile
{
    private DealDeliverableFile()
    {
    }

    public Guid Id { get; private set; }
    public Guid DeliverableId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }

    public DealDeliverable Deliverable { get; private set; } = null!;

    public static DealDeliverableFile Create(
        Guid deliverableId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes) => new()
    {
        Id = Guid.NewGuid(),
        DeliverableId = deliverableId,
        StorageKey = storageKey,
        FileName = fileName,
        ContentType = contentType,
        SizeBytes = sizeBytes
    };
}
