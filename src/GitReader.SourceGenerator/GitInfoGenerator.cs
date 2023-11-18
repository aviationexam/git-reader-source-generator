using GitReader.Structures;
using H.Generators;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitReader.SourceGenerator;

[Generator]
public class GitInfoGenerator : IIncrementalGenerator, IIncrementalGeneratorFactory<GitInfoGenerator>
{
    public const string Id = "GI";

    private readonly TimeProvider _timeProvider;

    public GitInfoGenerator() : this(TimeProvider.System)
    {
    }

    protected GitInfoGenerator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
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

    private static GitInfo? _gitInfoCache;

    private async Task<FileWithName> GetSourceCodeAsync(
        GitInfoConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        const string fileName = "GitInfo.g.cs";

        var gitInfoTuple = await LoadGitInfoAsync(configuration, cancellationToken);

        var (gitInfo, cacheHit) = gitInfoTuple ?? (gitInfo: new GitInfo(
            DateTimeOffset.Now,
            configuration.TargetNamespace,
            "Unknown",
            "123456789",
            "Unknown",
            DateTimeOffset.MinValue,
            TagName: null,
            Tag: null,
            TagVersion: null
        ), cacheHit: false);

        return new FileWithName(
            fileName,
            GetFileContent(gitInfo, cacheHit)
        );
    }

    private async Task<(GitInfo gitInfo, bool cacheHit)?> LoadGitInfoAsync(
        GitInfoConfiguration configuration,
        CancellationToken cancellationToken
    )
    {
        if (configuration.UseAggressiveCache && _gitInfoCache is not null)
        {
            return (_gitInfoCache, true);
        }

        using var repository = await Repository.Factory.OpenStructureAsync(
            configuration.RootDirectory,
            cancellationToken
        );

        if (
            repository.Head is { } head
            && await repository.GetCommitAsync(head.Head, cancellationToken) is { } commit
        )
        {
            if (_gitInfoCache is { } gitInfoCache && gitInfoCache.CommitHash == head.Head.ToString())
            {
                return (gitInfoCache, true);
            }

            var commitHash = commit.Hash.ToString();
            var commitCommitter = commit.Committer;
            var commitAbbreviatedHash = commitHash;

            if (commitAbbreviatedHash.Length > configuration.CommitAbbreviatedLength)
            {
                commitAbbreviatedHash = commitHash[..configuration.CommitAbbreviatedLength];
            }

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
                _timeProvider.GetUtcNow(),
                configuration.TargetNamespace,
                head.Name,
                commitAbbreviatedHash,
                commitHash,
                commitCommitter.Date,
                tagName,
                tag,
                tagVersion
            );

            if (configuration.UseCache)
            {
                _gitInfoCache = gitInfoCache;
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
        GitInfo gitInfo, bool cacheHit
    ) => $$"""
           // <auto-generated />

           // cacheHit: {{cacheHit}}
           // gitInformationRead: {{gitInfo.GitInformationRead:u}}

           #nullable enable

           using System;

           namespace {{gitInfo.Namespace}};

           public static class GitInfo
           {
               public const string Branch = "{{gitInfo.Branch}}";

               public const string CommitAbbreviatedHash = "{{gitInfo.CommitAbbreviatedHash}}";

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
        string Namespace,
        string Branch,
        string CommitAbbreviatedHash,
        string CommitHash,
        DateTimeOffset CommitDate,
        string? TagName,
        Tag? Tag,
        Version? TagVersion
    );
}
