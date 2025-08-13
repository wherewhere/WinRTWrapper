using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                            static (expression, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncOperation")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))),
                            static (expression, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsTask")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))),
                            static (expression, args, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo"),
                                    SyntaxFactory.IdentifierName("Run")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.AnonymousMethodExpression(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SeparatedList(args.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name)).WithType(SyntaxFactory.IdentifierName( x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.SingletonList(
                                                        SyntaxFactory.ReturnStatement(
                                                            SyntaxFactory.ParenthesizedExpression(expression(args)))))))))),
                            static (expression, args, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsTask")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        System.Linq.Enumerable.Concat([
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.ParenthesizedExpression(expression))],
                                            args.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name))))))),
                            taskOfT.TypeArguments,
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalGenericType(
                            taskOfT,
                            asyncOperation,
                            static (expression, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncOperation")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))),
                            static (expression, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsTask")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))),
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
                            static expression => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncAction")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))),
                            static expression => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsTask")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))),
                            static (expression, args) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo"),
                                    SyntaxFactory.IdentifierName("Run")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.AnonymousMethodExpression(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SeparatedList(args.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name)).WithType(SyntaxFactory.IdentifierName(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.SingletonList(
                                                        SyntaxFactory.ReturnStatement(
                                                            SyntaxFactory.ParenthesizedExpression(expression(args)))))))))),
                            static (expression, args) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsTask")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                        System.Linq.Enumerable.Concat([
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.ParenthesizedExpression(expression))],
                                            args.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name))))))),
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalType(
                            task,
                            asyncAction,
                            static expression => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncAction")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))),
                            static expression => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsTask")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.ParenthesizedExpression(expression))))));
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
                            static (expression, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncOperation")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParenthesizedExpression(expression),
                                                    SyntaxFactory.IdentifierName("AsTask"))))))),
                            static (expression, typeArgs) => SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.GenericName(
                                    SyntaxFactory.Identifier("global::System.Threading.Tasks.ValueTask"),
                                    SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SeparatedList<TypeSyntax>(
                                            typeArgs.Select(x =>
                                                SyntaxFactory.IdentifierName(
                                                    x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                                    SyntaxFactory.IdentifierName("AsTask")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.ParenthesizedExpression(expression)))))))),
                                default),
                            static (expression, args, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo"),
                                    SyntaxFactory.IdentifierName("Run")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.AnonymousMethodExpression(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SeparatedList(args.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name)).WithType(SyntaxFactory.IdentifierName(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.SingletonList(
                                                        SyntaxFactory.ReturnStatement(
                                                            SyntaxFactory.InvocationExpression(
                                                                SyntaxFactory.MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    SyntaxFactory.ParenthesizedExpression(expression(args)),
                                                                    SyntaxFactory.IdentifierName("AsTask"))))))))))),
                            static (expression, args, typeArgs) => SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.GenericName(
                                    SyntaxFactory.Identifier("global::System.Threading.Tasks.ValueTask"),
                                    SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SeparatedList<TypeSyntax>(
                                            typeArgs.Select(x =>
                                                SyntaxFactory.IdentifierName(
                                                    x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                                    SyntaxFactory.IdentifierName("AsTask")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SeparatedList(
                                                        System.Linq.Enumerable.Concat([
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.ParenthesizedExpression(expression))],
                                                            args.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name)))))))))),
                                default),
                            valueTaskOfT.TypeArguments,
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalGenericType(
                            valueTaskOfT,
                            asyncOperation,
                            static (expression, _) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncOperation")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParenthesizedExpression(expression),
                                                    SyntaxFactory.IdentifierName("AsTask"))))))),
                            static (expression, typeArgs) => SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.GenericName(
                                    SyntaxFactory.Identifier("global::System.Threading.Tasks.ValueTask"),
                                    SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SeparatedList<TypeSyntax>(
                                            typeArgs.Select(x =>
                                                SyntaxFactory.IdentifierName(
                                                    x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                                    SyntaxFactory.IdentifierName("AsTask")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.ParenthesizedExpression(expression)))))))),
                                default),
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
                            static expression => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncAction")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParenthesizedExpression(expression),
                                                    SyntaxFactory.IdentifierName("AsTask"))))))),
                            static expression => SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName("global::System.Threading.Tasks.ValueTask"),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                                    SyntaxFactory.IdentifierName("AsTask")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.ParenthesizedExpression(expression)))))))),
                                default),
                            static (expression, args) => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.Runtime.InteropServices.WindowsRuntime.AsyncInfo"),
                                    SyntaxFactory.IdentifierName("Run")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.AnonymousMethodExpression(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SeparatedList(args.Select(x => SyntaxFactory.Parameter(SyntaxFactory.Identifier(x.Name)).WithType(SyntaxFactory.IdentifierName(x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))))),
                                                SyntaxFactory.Block(
                                                    SyntaxFactory.SingletonList(
                                                        SyntaxFactory.ReturnStatement(
                                                            SyntaxFactory.InvocationExpression(
                                                                SyntaxFactory.MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    SyntaxFactory.ParenthesizedExpression(expression(args)),
                                                                    SyntaxFactory.IdentifierName("AsTask"))))))))))),
                            static (expression, args) => SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName("global::System.Threading.Tasks.ValueTask"),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                                    SyntaxFactory.IdentifierName("AsTask")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SeparatedList(
                                                        System.Linq.Enumerable.Concat([
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.ParenthesizedExpression(expression))],
                                                            args.Select(x => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(x.Name)))))))))),
                                default),
                            cancellationToken);
                    }
                    else
                    {
                        yield return new MarshalType(
                            valueTask,
                            asyncAction,
                            static expression => SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                    SyntaxFactory.IdentifierName("AsAsyncAction")),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.ParenthesizedExpression(expression),
                                                    SyntaxFactory.IdentifierName("AsTask"))))))),
                            static expression => SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName("global::System.Threading.Tasks.ValueTask"),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.InvocationExpression(
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName("global::System.WindowsRuntimeSystemExtensions"),
                                                    SyntaxFactory.IdentifierName("AsTask")),
                                                SyntaxFactory.ArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.ParenthesizedExpression(expression)))))))),
                                default));
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
                        static expression => SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("global::System.IO.WindowsRuntimeStreamExtensions"),
                                SyntaxFactory.IdentifierName("AsRandomAccessStream")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.ParenthesizedExpression(expression))))),
                        static expression => SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("global::System.IO.WindowsRuntimeStreamExtensions"),
                                SyntaxFactory.IdentifierName("AsStream")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.ParenthesizedExpression(expression))))));
                }

                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Storage.Streams.IOutputStream", out INamedTypeSymbol? outputStream))
                {
                    yield return new MarshalType(
                        stream,
                        outputStream,
                        static expression => SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("global::System.IO.WindowsRuntimeStreamExtensions"),
                                SyntaxFactory.IdentifierName("AsOutputStream")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.ParenthesizedExpression(expression))))),
                        static expression => SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("global::System.IO.WindowsRuntimeStreamExtensions"),
                                SyntaxFactory.IdentifierName("AsStreamForWrite")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.ParenthesizedExpression(expression))))));
                }

                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Storage.Streams.IInputStream", out INamedTypeSymbol? inputStream))
                {
                    yield return new MarshalType(
                        stream,
                        inputStream,
                        static expression => SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("global::System.IO.WindowsRuntimeStreamExtensions"),
                                SyntaxFactory.IdentifierName("AsInputStream")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.ParenthesizedExpression(expression))))),
                        static expression => SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("global::System.IO.WindowsRuntimeStreamExtensions"),
                                SyntaxFactory.IdentifierName("AsStreamForRead")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.ParenthesizedExpression(expression))))));
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Drawing.PointF", out INamedTypeSymbol? pointF))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.Point", out INamedTypeSymbol? point))
                {
                    yield return new MarshalType(
                        pointF,
                        point,
                        static expression => SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("global::Windows.Foundation.Point"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList([
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("X"))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("Y")))])),
                            default),
                        static expression => SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("global::System.Drawing.PointF"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList([
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("X")))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("Y"))))])),
                            default));
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Drawing.SizeF", out INamedTypeSymbol? sizeF))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.Size", out INamedTypeSymbol? size))
                {
                    yield return new MarshalType(
                        sizeF,
                        size,
                        static expression => SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("global::Windows.Foundation.Size"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList([
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("Width"))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("Height")))])),
                            default),
                        static expression => SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("global::System.Drawing.SizeF"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList([
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("Width")))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("Height"))))])),
                            default));
                }
            }

            if (compilation.GetAccessibleTypeWithMetadataName("System.Drawing.RectangleF", out INamedTypeSymbol? rectangle))
            {
                if (compilation.GetAccessibleTypeWithMetadataName("Windows.Foundation.Rect", out INamedTypeSymbol? rect))
                {
                    yield return new MarshalType(
                        rectangle,
                        rect,
                        static expression => SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("global::Windows.Foundation.Rect"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList([
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("X"))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("Y"))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("Width"))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ParenthesizedExpression(expression),
                                            SyntaxFactory.IdentifierName("Height")))])),
                            default),
                        static expression => SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("global::System.Drawing.RectangleF"),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList([
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("X")))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("Y")))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("Width")))),
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ParenthesizedExpression(expression),
                                                SyntaxFactory.IdentifierName("Height"))))])),
                            default));
                }
            }
        }
    }
}
