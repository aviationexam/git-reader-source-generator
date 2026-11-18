using GitReader.Structures;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.SourceGenerator;

[Generator]
public class GitInfoGenerator(
    TimeProvider timeProvider
) : IIncrementalGenerator
{
    public const string Id = "GI";

    public GitInfoGenerator() : this(TimeProvider.System)
    {
    }

    public static GitInfoGenerator Create(TimeProvider timeProvider) => new(
        timeProvider
    );

    public void Initialize(
        IncrementalGeneratorInitializationContext context
    ) => context.AnalyzerConfigOptionsProvider
        .Select(static (x, _) => new GitInfoConfiguration(
            x.GetRequiredGlobalOption("GitInfo_RootDirectory"),
            x.GetRequiredGlobalOption("GitInfo_Namespace"),
            x.GetGlobalOption("GitInfo_CommitAbbreviatedLength") is { } commitAbbreviatedString
            && int.TryParse(commitAbbreviatedString, out var commitAbbreviatedLength)
                ? commitAbbreviatedLength
                : 9,
            // ReSharper disable once SimplifyConditionalTernaryExpression
            x.GetGlobalOption("GitInfo_UseCache") is { } useCacheString
            && bool.TryParse(useCacheString, out var useCache)
                ? useCache
                : true,
            // ReSharper disable once SimplifyConditionalTernaryExpression
            x.GetGlobalOption("GitInfo_UseAggressiveCache") is { } useAggressiveCacheString
            && bool.TryParse(useAggressiveCacheString, out var useAggressiveCache)
                ? useAggressiveCache
                : true
        ))
        .SelectAndReportExceptions(GetSourceCode, context, Id)
        .AddSource(context);

    private record GitInfoConfiguration(
        string RootDirectory,
        string TargetNamespace,
        int CommitAbbreviatedLength,
        bool UseCache,
        bool UseAggressiveCache
    );

    private FileWithName GetSourceCode(
        GitInfoConfiguration configuration,
        CancellationToken cancellationToken
    ) => GetSourceCodeAsync(configuration, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

    private static readonly ConcurrentDictionary<string, GitInfo> GitInfoCache = new(
        Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal
    );

    private async Task<FileWithName> GetSourceCodeAsync(
        GitInfoConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        const string fileName = "GitInfo.g.cs";

        var gitInfoTuple = await LoadGitInfoAsync(configuration, cancellationToken);

        var (gitInfo, cacheHit) = gitInfoTuple ?? (gitInfo: new GitInfo(
            DateTimeOffset.Now,
            "Unknown",
            "Unknown",
            DateTimeOffset.MinValue,
            TagName: null,
            Tag: null,
            TagVersion: null
        ), cacheHit: false);

        var commitAbbreviatedHash = gitInfoTuple is null
            ? "123456789"
            : gitInfo.CommitHash[..Math.Min(gitInfo.CommitHash.Length, configuration.CommitAbbreviatedLength)];

        return new FileWithName(
            fileName,
            GetFileContent(gitInfo, cacheHit, configuration.TargetNamespace, commitAbbreviatedHash)
        );
    }

    private async Task<(GitInfo gitInfo, bool cacheHit)?> LoadGitInfoAsync(
        GitInfoConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        var rootDirectory = Path.GetFullPath(configuration.RootDirectory);
        if (rootDirectory.Length > Path.GetPathRoot(rootDirectory)!.Length)
        {
            rootDirectory = rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        if (configuration.UseAggressiveCache && GitInfoCache.TryGetValue(rootDirectory, out var cached))
        {
            return (cached, true);
        }

        using var repository = await Repository.Factory.OpenStructureAsync(
            rootDirectory,
            new NonBlockingFileSystem(65536),
            cancellationToken
        );

        if (
            repository.Head is { } head
            && await repository.GetCommitAsync(head.Head, cancellationToken) is { } commit
        )
        {
            if (GitInfoCache.TryGetValue(rootDirectory, out var gitInfoCache) && gitInfoCache.CommitHash == head.Head.ToString())
            {
                return (gitInfoCache, true);
            }

            var commitHash = commit.Hash.ToString();
            var commitCommitter = commit.Committer;

            var tags = repository.Tags;

            string? tagName = null;
            Tag? tag = null;
            Version? tagVersion = null;

            while (commit != null)
            {
                var regLogHasTags = tags
                    .Where(x => commit.Hash.HashCode.SequenceEqual(x.Value.ObjectHash.HashCode))
                    .Select(x => (TagName: x.Key, Tag: x.Value, Version: ParseTag(x.Value.Name)))
                    .Where(x => x.Version is not null)
                    .ToList();

                if (regLogHasTags.Count >= 1)
                {
                    if (regLogHasTags.Count == 1)
                    {
                        (tagName, tag, tagVersion) = regLogHasTags.Single();

                        break;
                    }

                    (tagName, tag, tagVersion) = regLogHasTags.OrderBy(x => x.Version).Single();

                    break;
                }

                commit = await commit.GetPrimaryParentCommitAsync(cancellationToken);
            }

            gitInfoCache = new GitInfo(
                timeProvider.GetUtcNow(),
                head.Name,
                commitHash,
                commitCommitter.Date,
                tagName,
                tag,
                tagVersion
            );

            if (configuration.UseCache)
            {
                GitInfoCache[rootDirectory] = gitInfoCache;
            }

            return (gitInfoCache, false);
        }

        return null;
    }

    private static Version? ParseTag(string tag)
    {
        var tagName = tag.AsSpan();

        if (tagName.Length > 0 && tagName[0] == 'v')
        {
            tagName = tagName[1..];
        }

        foreach (var ch in tagName)
        {
            if (
                !char.IsDigit(ch)
                && ch != '.'
            )
            {
                return null;
            }
        }

        return new Version(tagName.ToString());
    }

    private static string GetFileContent(
        GitInfo gitInfo, bool cacheHit, string targetNamespace, string commitAbbreviatedHash
    ) => $$"""
           // <auto-generated />

           // cacheHit: {{cacheHit}}
           // gitInformationRead: {{gitInfo.GitInformationRead:u}}

           #nullable enable

           using System;

           namespace {{targetNamespace}};

           public static class GitInfo
           {
               public const string Branch = "{{gitInfo.Branch}}";

               public const string CommitAbbreviatedHash = "{{commitAbbreviatedHash}}";

               public const string CommitHash = "{{gitInfo.CommitHash}}";

               public static DateTimeOffset CommitDate = new DateTimeOffset({{gitInfo.CommitDate.Ticks}}, new TimeSpan({{gitInfo.CommitDate.Offset.Ticks}}));

               public static string? TagName = {{(gitInfo.TagName is null ? "null" : $"\"{gitInfo.TagName}\"")}};

               public static string? Tag = {{(gitInfo.Tag is null ? "null" : $"\"{gitInfo.Tag.Name}\"")}};

               public static int? TagMajor = {{(gitInfo.TagVersion is null || gitInfo.TagVersion.Major is -1 ? "null" : $"{gitInfo.TagVersion.Major}")}};

               public static int? TagMinor = {{(gitInfo.TagVersion is null || gitInfo.TagVersion.Minor is -1 ? "null" : $"{gitInfo.TagVersion.Minor}")}};

               public static int? TagBuild = {{(gitInfo.TagVersion is null || gitInfo.TagVersion.Build is -1 ? "null" : $"{gitInfo.TagVersion.Build}")}};

               public static int? TagRevision = {{(gitInfo.TagVersion is null || gitInfo.TagVersion.Revision is -1 ? "null" : $"{gitInfo.TagVersion.Revision}")}};
           }

           #nullable restore
           """;

    private record GitInfo(
        DateTimeOffset GitInformationRead,
        string Branch,
        string CommitHash,
        DateTimeOffset CommitDate,
        string? TagName,
        Tag? Tag,
        Version? TagVersion
    );
}
