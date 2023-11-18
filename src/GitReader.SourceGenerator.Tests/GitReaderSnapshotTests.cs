using H.Generators.Tests.Extensions;
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
    public Task VTagRepositoryWorks() => TestHelper.Verify<GitInfoGenerator>(
        new DateTimeOffset(2023, 11, 18, 0, 1, 2, TimeSpan.Zero),
        new DictionaryAnalyzerConfigOptionsProvider(globalOptions: new Dictionary<string, string>
        {
            ["build_property.GitInfo_RootDirectory"] = "v-tag-repository".GetRepositoryPath(),
            ["build_property.GitInfo_Namespace"] = "ApplicationNamespace",
        })
    );
}
