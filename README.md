[![Build Status](https://github.com/aviationexam/git-reader-source-generator/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/aviationexam/git-reader-source-generator/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/GitReader.SourceGenerator.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/GitReader.SourceGenerator/)
[![MyGet](https://img.shields.io/myget/git-reader-source-generator/vpre/GitReader.SourceGenerator?label=MyGet)](https://www.myget.org/feed/git-reader-source-generator/package/nuget/GitReader.SourceGenerator)
[![feedz.io](https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Faviationexam%2Fgit-reader-source-generator%2Fshield%2FGitReader.SourceGenerator%2Flatest&label=GitReader.SourceGenerator)](https://f.feedz.io/aviationexam/git-reader-source-generator/packages/GitReader.SourceGenerator/latest/download)

# GitReader.SourceGenerator

This library build on top of [kekyo/GitReader](https://github.com/kekyo/GitReader), which is "Lightweight Git local repository traversal library".

## Install
```xml
<ItemGroup>
    <PackageReference Include="GitReader.SourceGenerator" Version="0.1.4" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />
</ItemGroup>
```

## How to configure library

```xml
<PropertyGroup Label="Configure GitInfo">
    <!-- optional, default value: $([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), '.gitignore')) -->
    <GitInfo_RootDirectory>$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'My.Project.slnx'))\..\.git</GitInfo_RootDirectory>

    <GitInfo_Namespace>My.Project.GitInfoNamespace</GitInfo_Namespace>

    <!-- optional, default value 9 -->
    <GitInfo_CommitAbbreviatedLength>9</GitInfo_CommitAbbreviatedLength>

    <!-- optional, default value true -->
    <GitInfo_UseCache>true</GitInfo_UseCache>

    <!-- optional, default value false -->
    <!-- it check only cache existence, not that HEAD equals HEAD in cache -->
    <GitInfo_UseAggressiveCache>true</GitInfo_UseAggressiveCache>
</PropertyGroup>
```

## How to use library

```cs
using My.Project.GitInfoNamespace;

Console.WriteLine(GitInfo.Branch);
Console.WriteLine(GitInfo.CommitAbbreviatedHash);
Console.WriteLine(GitInfo.CommitHash);
Console.WriteLine(GitInfo.CommitDate);
Console.WriteLine(GitInfo.TagName);
Console.WriteLine(GitInfo.Tag);
Console.WriteLine(GitInfo.TagMajor);
Console.WriteLine(GitInfo.TagMinor);
Console.WriteLine(GitInfo.TagBuild);
Console.WriteLine(GitInfo.TagRevision);
```
