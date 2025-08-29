namespace WinRTWrapper.SourceGenerators.Models
{
    /// <summary>
    /// A model to hold information on a type to generate.
    /// </summary>
    /// <param name="Name">The file name to generate.</param>
    /// <param name="Source">The code to generate.</param>
    internal sealed record GeneratedType(string Name, string Source);
}
