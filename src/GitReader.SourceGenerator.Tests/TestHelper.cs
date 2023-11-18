using H.Generators.Tests.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VerifyXunit;

namespace GitReader.SourceGenerator.Tests;

public static class TestHelper
{
    private const string RepositorySnapshots = "repository-snapshots";

    private static string? _rootDirectory;

    public static string GetRepositoryPath(this string repository)
    {
        _rootDirectory ??= Directory.GetParent(
            Directory.GetCurrentDirectory()
        )?.Parent?.Parent?.FullName;

        return Path.Join(_rootDirectory, RepositorySnapshots, repository);
    }

    public static Task Verify<TIncrementalGenerator>(
        TIncrementalGenerator generator,
        [StringSyntax("csharp")] params string[] source
    ) where TIncrementalGenerator : class, IIncrementalGenerator => Verify(
        generator,
        new DictionaryAnalyzerConfigOptionsProvider(),
        source
    );

    public static Task Verify<TIncrementalGenerator>(
        TIncrementalGenerator generator,
        DictionaryAnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        [StringSyntax("csharp")] params string[] source
    ) where TIncrementalGenerator : class, IIncrementalGenerator
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTrees = source.Select(x => CSharpSyntaxTree.ParseText(x)).ToArray();

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: new[]
                {
                    typeof(object).Assembly.Location,
                }
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToArray()
        );

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            optionsProvider: analyzerConfigOptionsProvider
        );

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver);
    }
}
