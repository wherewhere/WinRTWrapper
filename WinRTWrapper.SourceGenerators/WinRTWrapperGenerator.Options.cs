using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="source">The source tuple containing the <see cref="AnalyzerConfigOptionsProvider"/> and <see cref="Compilation"/>.</param>
        /// <param name="_">The cancellation token for the operation.</param>
        /// <returns>The <see cref="GenerationOptions"/> for the current generation.</returns>
        private static GenerationOptions GetGenerationOptions((AnalyzerConfigOptionsProvider options, Compilation compilation) source, CancellationToken _)
        {
            (AnalyzerConfigOptionsProvider options, Compilation compilation) = source;
            bool isWinMDObject = options.GetStringMSBuildProperty(MSBuildProperties.OutputType).Equals("winmdobj", StringComparison.OrdinalIgnoreCase);
            bool isWinRTComponent = options.GetBoolMSBuildProperty(MSBuildProperties.CsWinRTComponent);
            bool isCSWinRT = compilation.ReferencedAssemblyNames.Any(x => x.Name.Equals("WinRT.Runtime", StringComparison.OrdinalIgnoreCase));
            IEnumerable<IMarshalType> marshals = CreateMarshallers(compilation);
            return new GenerationOptions(isWinMDObject, isWinRTComponent, isCSWinRT, [.. marshals]);
        }

        private static IEnumerable<IMarshalType> CreateMarshallers(Compilation compilation)
        {
            if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.Tasks.Task`1", out INamedTypeSymbol? taskOfT))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.IAsyncOperation`1", out INamedTypeSymbol? asyncOperation))
                {
                    if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.CancellationToken", out INamedTypeSymbol? cancellationToken))
                    {
                        yield return new MarshalGenericTypeWithArgs(
                            taskOfT,
                            asyncOperation,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncOperation(({inner}))",
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsTask(({inner}))",
                            static (inner, args) => $"global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo.Run(delegate ({string.Join(", ", args.Select(x => $"{x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {x.Name}"))}) {{ return {inner(args)}; }})",
                            static (inner, args) => $"global::System.WindowsRuntimeSystemExtensions.AsTask(({inner}, {string.Join(", ", args.Select(x => x.Name))}))",
                            taskOfT.TypeArguments,
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalGenericType(
                            taskOfT,
                            asyncOperation,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncOperation(({inner}))",
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsTask(({inner}))",
                            taskOfT.TypeArguments);
                    }
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.Tasks.Task", out INamedTypeSymbol? task))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.IAsyncAction", out INamedTypeSymbol? asyncAction))
                {
                    if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.CancellationToken", out INamedTypeSymbol? cancellationToken))
                    {
                        yield return new MarshalTypeWithArgs(
                            task,
                            asyncAction,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncAction({inner})",
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsTask({inner})",
                            static (inner, args) => $"global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo.Run(delegate ({string.Join(", ", args.Select(x => $"{x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {x.Name}"))}) {{ return {inner(args)}; }})",
                            static (inner, args) => $"global::System.WindowsRuntimeSystemExtensions.AsTask(({inner}, {string.Join(", ", args.Select(x => x.Name))}))",
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalType(
                            task,
                            asyncAction,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncAction({inner})",
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsTask({inner})");
                    }
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.Tasks.ValueTask`1", out INamedTypeSymbol? valueTaskOfT))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.IAsyncOperation`1", out INamedTypeSymbol? asyncOperation))
                {
                    if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.CancellationToken", out INamedTypeSymbol? cancellationToken))
                    {
                        yield return new MarshalGenericTypeWithArgs(
                            valueTaskOfT,
                            asyncOperation,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncOperation(({inner}).AsTask())",
                            static inner => $"new global::System.Threading.Tasks.ValueTask(global::System.WindowsRuntimeSystemExtensions.AsTask({inner}))",
                            static (inner, args) => $"global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo.Run(delegate ({string.Join(", ", args.Select(x => $"{x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {x.Name}"))}) {{ return ({inner(args)}).AsTask(); }})",
                            static (inner, args) => $"new global::System.Threading.Tasks.ValueTask(global::System.WindowsRuntimeSystemExtensions.AsTask(({inner}, {string.Join(", ", args.Select(x => x.Name))})))",
                            valueTaskOfT.TypeArguments,
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalGenericType(
                            valueTaskOfT,
                            asyncOperation,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncOperation(({inner}).AsTask())",
                            static inner => $"new global::System.Threading.Tasks.ValueTask(global::System.WindowsRuntimeSystemExtensions.AsTask({inner}))",
                            valueTaskOfT.TypeArguments);
                    }
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.Tasks.ValueTask", out INamedTypeSymbol? valueTask))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.IAsyncAction", out INamedTypeSymbol? asyncAction))
                {
                    if (compilation.GetAccessibleTypeWithMetadataName("System.Threading.CancellationToken", out INamedTypeSymbol? cancellationToken))
                    {
                        yield return new MarshalTypeWithArgs(
                            valueTask,
                            asyncAction,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncAction(({inner}).AsTask())",
                            static inner => $"new global::System.Threading.Tasks.ValueTask(global::System.WindowsRuntimeSystemExtensions.AsTask({inner}))",
                            static (inner, args) => $"global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo.Run(delegate ({string.Join(", ", args.Select(x => $"{x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {x.Name}"))}) {{ return ({inner(args)}).AsTask(); }})",
                            static (inner, args) => $"new global::System.Threading.Tasks.ValueTask(global::System.WindowsRuntimeSystemExtensions.AsTask(({inner}, {string.Join(", ", args.Select(x => x.Name))})))",
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalType(
                            valueTask,
                            asyncAction,
                            static inner => $"global::System.WindowsRuntimeSystemExtensions.AsAsyncAction(({inner}).AsTask())",
                            static inner => $"new global::System.Threading.Tasks.ValueTask(global::System.WindowsRuntimeSystemExtensions.AsTask({inner}))");
                    }
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.IO.Stream", out INamedTypeSymbol? stream))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Storage.Streams.IRandomAccessStream", out INamedTypeSymbol? randomAccessStream))
                {
                    yield return new MarshalType(
                        stream,
                        randomAccessStream,
                        static inner => $"global::System.IO.WindowsRuntimeStreamExtensions.AsRandomAccessStream({inner})",
                        static inner => $"global::System.IO.WindowsRuntimeStreamExtensions.AsStream({inner})");
                }

                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Storage.Streams.IOutputStream", out INamedTypeSymbol? outputStream))
                {
                    yield return new MarshalType(
                        stream,
                        outputStream,
                        static inner => $"global::System.IO.WindowsRuntimeStreamExtensions.AsOutputStream({inner})",
                        static inner => $"global::System.IO.WindowsRuntimeStreamExtensions.AsStreamForWrite({inner})");
                }

                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Storage.Streams.IInputStream", out INamedTypeSymbol? inputStream))
                {
                    yield return new MarshalType(
                        stream,
                        inputStream,
                        static inner => $"global::System.IO.WindowsRuntimeStreamExtensions.AsInputStream({inner})",
                        static inner => $"global::System.IO.WindowsRuntimeStreamExtensions.AsStreamForRead({inner})");
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Drawing.PointF", out INamedTypeSymbol? point))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.Point", out INamedTypeSymbol? _point))
                {
                    yield return new MarshalType(
                        point,
                        _point,
                        static inner => $"new global::Windows.Foundation.Point(({inner}).X, ({inner}).Y)",
                        static inner => $"new global::System.Drawing.PointF((float)({inner}).X, (float)({inner}).Y)");
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Drawing.SizeF", out INamedTypeSymbol? size))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.Size", out INamedTypeSymbol? _size))
                {
                    yield return new MarshalType(
                        size,
                        _size,
                        static inner => $"new global::Windows.Foundation.Size(({inner}).Width, ({inner}).Height)",
                        static inner => $"new global::System.Drawing.SizeF((float)({inner}).Width, (float)({inner}).Height)");
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Drawing.RectangleF", out INamedTypeSymbol? rectangle))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.Rect", out INamedTypeSymbol? rect))
                {
                    yield return new MarshalType(
                        rectangle,
                        rect,
                        static inner => $"new global::Windows.Foundation.Rect(({inner}).X, ({inner}).Y, ({inner}).Width, ({inner}).Height)",
                        static inner => $"new global::System.Drawing.RectangleF((float)({inner}).X, (float)({inner}).Y, (float)({inner}).Width, (float)({inner}).Height)");
                }
            }
        }
    }
}
