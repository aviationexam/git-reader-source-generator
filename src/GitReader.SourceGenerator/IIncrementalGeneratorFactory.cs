using System.Diagnostics.CodeAnalysis;
#if NET7_0_OR_GREATER
using System;
#endif

namespace GitReader.SourceGenerator;

public interface IIncrementalGeneratorFactory<
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
T
>
{
#if NET7_0_OR_GREATER
    static abstract T Create(TimeProvider timeProvider);
#endif
}
