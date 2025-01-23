using GitReader.IO;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.SourceGenerator;

[SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers")]
internal sealed class NonBlockingFileSystem(
    int bufferSize
) : IFileSystem
{
    private readonly IFileSystem _fileSystem = new StandardFileSystem(bufferSize);

    public string Combine(
        params string[] paths
    ) => _fileSystem.Combine(paths);

    public string GetDirectoryPath(
        string path
    ) => _fileSystem.GetDirectoryPath(path);

    public string GetFullPath(
        string path
    ) => _fileSystem.GetFullPath(path);

    public bool IsPathRooted(
        string path
    ) => _fileSystem.IsPathRooted(path);

    public string ResolveRelativePath(
        string basePath, string path
    ) => _fileSystem.ResolveRelativePath(basePath, path);

    public Task<bool> IsFileExistsAsync(
        string path, CancellationToken ct
    ) => _fileSystem.IsFileExistsAsync(path, ct);

    public Task<string[]> GetFilesAsync(
        string basePath, string match, CancellationToken ct
    ) => _fileSystem.GetFilesAsync(basePath, match, ct);

    public Task<Stream> OpenAsync(
        string path, bool isSeekable, CancellationToken ct
    ) => Task.FromResult<Stream>(new FileStream(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite,
        65536,
        true
    ));

    public Task<TemporaryFileDescriptor> CreateTemporaryAsync(
        CancellationToken ct
    ) => _fileSystem.CreateTemporaryAsync(ct);
}
