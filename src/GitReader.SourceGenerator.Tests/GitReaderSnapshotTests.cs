using H.Generators.Tests.Extensions;
using Microsoft.Extensions.Time.Testing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace GitReader.SourceGenerator.Tests;

[UsesVerify]
public class GitReaderSnapshotTests
{
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
