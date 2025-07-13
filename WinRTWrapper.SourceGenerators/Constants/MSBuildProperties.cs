namespace WinRTWrapper.SourceGenerators.Constants
{
    /// <summary>
    /// Exposes the available PolySharp MSBuild properties.
    /// </summary>
    internal static class MSBuildProperties
    {
        /// <summary>
        /// The type of output from the compiler, such as an executable or library.
        /// </summary>
        public const string OutputType = nameof(OutputType);

        /// <summary>
        /// Whether the project is a WinRT component.
        /// </summary>
        public const string CsWinRTComponent = nameof(CsWinRTComponent);
    }
}
