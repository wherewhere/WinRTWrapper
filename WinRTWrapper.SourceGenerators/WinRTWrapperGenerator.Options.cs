using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Threading;
using WinRTWrapper.SourceGenerators.Constants;
using WinRTWrapper.SourceGenerators.Extensions;
using WinRTWrapper.SourceGenerators.Models;

namespace WinRTWrapper.SourceGenerators
{
    public partial class WinRTWrapperGenerator
    {
        /// <summary>
        /// Extracts the <see cref="GenerationOptions"/> value for the current generation.
        /// </summary>
        /// <param name="options">The input <see cref="AnalyzerConfigOptionsProvider"/> instance.</param>
        /// <param name="_">The cancellation token for the operation.</param>
        /// <returns>The <see cref="GenerationOptions"/> for the current generation.</returns>
        private static GenerationOptions GetGenerationOptions(AnalyzerConfigOptionsProvider options, CancellationToken _)
        {
            bool isWinMDObject = options.GetStringMSBuildProperty(MSBuildProperties.OutputType).Equals("winmdobj", StringComparison.OrdinalIgnoreCase);
            return new GenerationOptions(isWinMDObject);
        }
    }
}
