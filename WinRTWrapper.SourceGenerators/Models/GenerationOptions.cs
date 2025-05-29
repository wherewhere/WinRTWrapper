namespace WinRTWrapper.SourceGenerators.Models
{
    /// <summary>
    /// A model to hold all generation options for the source generator.
    /// </summary>
    /// <param name="IsWinMDObject">Whether the output type is a WinMD object.</param>
    internal sealed record GenerationOptions(bool IsWinMDObject);
}
