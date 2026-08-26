namespace Web.Domain.Entities;

public class ProjectAttachment
{
    private ProjectAttachment()
    {
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Project Project { get; private set; } = null!;

    public static ProjectAttachment Create(
        Guid projectId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        StorageKey = storageKey,
        FileName = fileName,
        ContentType = contentType,
        SizeBytes = sizeBytes,
        CreatedAt = utcNow
    };
}
