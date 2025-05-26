using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WinRTWrapper.SourceGenerators.Extensions;
using WinRTWrapper.SourceGenerators.Helpers;
using WinRTWrapper.SourceGenerators.Models;

namespace WinRTWrapper.SourceGenerators
{
    public partial class WinRTWrapperGenerator
    {
        /// <summary>
        /// A regex to extract the fully qualified type name of a type from its embedded resource name.
        /// </summary>
        private const string EmbeddedResourceNameToFullyQualifiedTypeNameRegex = @"^WinRTWrapper\.SourceGenerators\.EmbeddedResources\.(\w+)\.cs$";

        /// <summary>
        /// The mapping of fully qualified type names to embedded resource names.
        /// </summary>
        public static readonly ImmutableDictionary<string, string> FullyQualifiedTypeNamesToResourceNames = ImmutableDictionary.CreateRange(
            from string resourceName in typeof(WinRTWrapperGenerator).Assembly.GetManifestResourceNames()
            select new KeyValuePair<string, string>(Regex.Match(resourceName, EmbeddedResourceNameToFullyQualifiedTypeNameRegex).Groups[1].Value, resourceName));

        /// <summary>
        /// The collection of all fully qualified type names for available types.
        /// </summary>
        private static readonly ImmutableArray<string> AllSupportTypeNames = [.. FullyQualifiedTypeNamesToResourceNames.Keys];

        /// <summary>
        /// The dictionary of cached sources to produce.
        /// </summary>
        private readonly ConcurrentDictionary<string, SourceText> manifestSources = new();

        /// <summary>
        /// Emits the source for a given <see cref="GeneratedType"/> object.
        /// </summary>
        /// <param name="context">The input <see cref="IncrementalGeneratorPostInitializationContext"/> instance to use to emit code.</param>
        /// <param name="type">The <see cref="GeneratedType"/> object with info on the source to emit.</param>
        private void EmitPostGeneratedType(IncrementalGeneratorPostInitializationContext context)
        {
            // Inspect all available types and filter them down according to the current compilation
            foreach (string name in AllSupportTypeNames)
            {
                // Get the source text from the cache, or load it if needed
                if (!manifestSources.TryGetValue(name, out SourceText? sourceText))
                {
                    string resourceName = FullyQualifiedTypeNamesToResourceNames[name];

                    using Stream stream = typeof(WinRTWrapperGenerator).Assembly.GetManifestResourceStream(resourceName);

                    // If the default accessibility is used, we can load the source directly
                    sourceText = SourceText.From(stream, Encoding.UTF8, canBeEmbedded: true);

                    // Cache the generated source (if we raced against another thread, just discard the result)
                    _ = manifestSources.TryAdd(name, sourceText);
                }

                // Finally generate the source text
                context.AddSource($"{name}.g.cs", sourceText);
            }
        }
    }
}
