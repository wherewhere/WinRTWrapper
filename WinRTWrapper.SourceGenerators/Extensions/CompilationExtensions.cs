using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;

namespace WinRTWrapper.SourceGenerators.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="Compilation"/> type.
    /// </summary>
    internal static class CompilationExtensions
    {
        /// <summary>
        /// Checks whether a given compilation (assumed to be for C#) is using at least a given language version.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> to consider for analysis.</param>
        /// <param name="languageVersion">The minimum language version to check.</param>
        /// <returns>Whether <paramref name="compilation"/> is using at least the specified language version.</returns>
        public static bool HasLanguageVersionAtLeastEqualTo(this Compilation compilation, LanguageVersion languageVersion)
        {
            return (compilation as CSharpCompilation)?.LanguageVersion >= languageVersion;
        }

        /// <summary>
        /// Attempts to retrieve an accessible type symbol from the specified compilation by its fully qualified
        /// metadata name.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> instance to search for the type symbol.</param>
        /// <param name="fullyQualifiedMetadataName">The fully qualified metadata name of the type to locate.</param>
        /// <param name="symbol">When this method returns <see langword="true"/>, contains the accessible <see cref="INamedTypeSymbol"/> that
        /// matches the specified metadata name. Otherwise, contains <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if an accessible type symbol matching the specified metadata name is found;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool GetAccessibleTypeWithMetadataName(this Compilation compilation, string fullyQualifiedMetadataName, [NotNullWhen(true)] out INamedTypeSymbol? symbol)
        {
            // If there is only a single matching symbol, check its accessibility
            if (compilation.GetTypeByMetadataName(fullyQualifiedMetadataName) is INamedTypeSymbol typeSymbol)
            {
                symbol = typeSymbol;
                return compilation.IsSymbolAccessibleWithin(typeSymbol, compilation.Assembly);
            }

            // Otherwise, check all available types
            foreach (INamedTypeSymbol currentTypeSymbol in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
            {
                if (compilation.IsSymbolAccessibleWithin(currentTypeSymbol, compilation.Assembly))
                {
                    symbol = currentTypeSymbol;
                    return true;
                }
            }

            symbol = null;
            return false;
        }
    }
}