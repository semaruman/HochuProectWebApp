using Web.Common.Results;

namespace Web.Infrastructure.Files;

public interface IFileStorage
{
    Task<Result<StoredFile>> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}

public sealed record StoredFile(string StorageKey, string FileName, string ContentType, long SizeBytes);
