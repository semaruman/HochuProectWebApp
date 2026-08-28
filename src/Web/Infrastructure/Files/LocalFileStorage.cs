using Microsoft.Extensions.Options;
using Web.Common.Results;

namespace Web.Infrastructure.Files;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string Root { get; set; } = "App_Data/files";
    public long MaxFileBytes { get; set; } = 20 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } =
    [
        ".pdf", ".png", ".jpg", ".jpeg", ".zip", ".dxf", ".dwg", ".step", ".stp", ".stl"
    ];
}

public class LocalFileStorage(IOptions<FileStorageOptions> options, IWebHostEnvironment env) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    private string RootPath =>
        Path.IsPathRooted(_options.Root)
            ? _options.Root
            : Path.Combine(env.ContentRootPath, _options.Root);

    public async Task<Result<StoredFile>> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(ext))
            return ResultErrors.BadRequest($"File type '{ext}' is not allowed.");

        if (content.CanSeek && content.Length > _options.MaxFileBytes)
            return ResultErrors.BadRequest("File is too large.");

        var safeName = Path.GetFileName(fileName);
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(RootPath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        var size = fs.Length;
        if (size > _options.MaxFileBytes)
        {
            fs.Close();
            File.Delete(fullPath);
            return ResultErrors.BadRequest("File is too large.");
        }

        return new StoredFile(key, safeName, contentType, size);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(RootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(RootPath, storageKey.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
