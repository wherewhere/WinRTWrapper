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

        /// <summary>
        /// The MSBuild property for including generated types of extensions.
        /// </summary>
        public const string WinRTWrapperIncludeGeneratedTypes = nameof(WinRTWrapperIncludeGeneratedTypes);
    }
}
