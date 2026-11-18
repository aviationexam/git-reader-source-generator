using H.Generators.Tests.Extensions;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using VerifyTests;
using Xunit;

namespace GitReader.SourceGenerator.Tests;

public class GitReaderSnapshotTests
{
    [Fact]
    public async Task AggressiveCacheSeparatesRepositories()
    {
        using var repositories = new TemporaryRepositories();
        await VerifyCached(repositories.Extract("tag-repository"), "ApplicationNamespace", aggressive: true)
            .UseTextForParameters("Tagged");
        await VerifyCached(repositories.Extract("no-tag-repository"), "ApplicationNamespace", aggressive: true)
            .UseTextForParameters("Untagged");
    }

    [Fact]
    public async Task CacheHitUsesCurrentNamespace()
    {
        using var repositories = new TemporaryRepositories();
        var repository = repositories.Extract("v-tag-repository");
        await VerifyCached(repository, "FirstNamespace", aggressive: false)
            .UseTextForParameters("First");
        await VerifyCached(repository, "SecondNamespace", aggressive: false)
            .UseTextForParameters("Second");
    }

    private static SettingsTask VerifyCached(string repository, string targetNamespace, bool aggressive) =>
        TestHelper.Verify(
            new GitInfoGenerator(new FakeTimeProvider(
                new DateTimeOffset(2023, 11, 18, 0, 1, 2, TimeSpan.Zero)
            )),
            new DictionaryAnalyzerConfigOptionsProvider(globalOptions: new Dictionary<string, string>
            {
                ["build_property.GitInfo_RootDirectory"] = repository,
                ["build_property.GitInfo_Namespace"] = targetNamespace,
                ["build_property.GitInfo_UseCache"] = "true",
                ["build_property.GitInfo_UseAggressiveCache"] = aggressive ? "true" : "false",
            })
        );

    private sealed class TemporaryRepositories : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string Extract(string repository)
        {
            ZipFile.ExtractToDirectory((repository + ".zip").GetRepositoryPath(), _directory.FullName);
            return Path.Combine(_directory.FullName, repository);
        }

        public void Dispose() => _directory.Delete(recursive: true);
    }

    [Fact]
    public Task VTagRepositoryWorks() => TestHelper.Verify(
        new GitInfoGenerator(new FakeTimeProvider(
            new DateTimeOffset(2023, 11, 18, 0, 1, 2, TimeSpan.Zero)
        )),
        new DictionaryAnalyzerConfigOptionsProvider(globalOptions: new Dictionary<string, string>
        {
            ["build_property.GitInfo_RootDirectory"] = "v-tag-repository".GetRepositoryPath(),
            ["build_property.GitInfo_Namespace"] = "ApplicationNamespace",
            ["build_property.GitInfo_UseCache"] = "false",
        })
    );

    [Fact]
    public Task TagRepositoryWorks() => TestHelper.Verify(
        new GitInfoGenerator(new FakeTimeProvider(
            new DateTimeOffset(2023, 11, 18, 0, 1, 2, TimeSpan.Zero)
        )),
        new DictionaryAnalyzerConfigOptionsProvider(globalOptions: new Dictionary<string, string>
        {
            ["build_property.GitInfo_RootDirectory"] = "tag-repository".GetRepositoryPath(),
            ["build_property.GitInfo_Namespace"] = "ApplicationNamespace",
            ["build_property.GitInfo_UseCache"] = "false",
        })
    );

    [Fact]
    public Task NoTagRepositoryWorks() => TestHelper.Verify(
        new GitInfoGenerator(new FakeTimeProvider(
            new DateTimeOffset(2023, 11, 18, 0, 1, 2, TimeSpan.Zero)
        )),
        new DictionaryAnalyzerConfigOptionsProvider(globalOptions: new Dictionary<string, string>
        {
            ["build_property.GitInfo_RootDirectory"] = "no-tag-repository".GetRepositoryPath(),
            ["build_property.GitInfo_Namespace"] = "ApplicationNamespace",
            ["build_property.GitInfo_UseCache"] = "false",
        })
    );
}
