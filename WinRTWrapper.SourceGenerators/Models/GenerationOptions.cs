using System.Collections.Immutable;

namespace WinRTWrapper.SourceGenerators.Models
{
    /// <summary>
    /// A model to hold all generation options for the source generator.
    /// </summary>
    /// <param name="IsWinMDObject">Whether the output type is a WinMD object.</param>
    /// <param name="IsCSWinRT">Whether the project is using CSWinRT.</param>
    /// <param name="Marshals">The collection of marshaling types used in the generation.</param>
    internal sealed record GenerationOptions(bool IsWinMDObject, bool IsCSWinRT, ImmutableArray<MarshalType> Marshals);
}
