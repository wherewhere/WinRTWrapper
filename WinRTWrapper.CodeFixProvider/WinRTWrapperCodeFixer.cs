using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace WinRTWrapper.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WinRTWrapperCodeFixer)), Shared]
    public sealed class WinRTWrapperCodeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ["WRAPPER001"];

        public sealed override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) { return; }

            SyntaxNode? node = root.FindNode(context.Span);
            if (node == null) { return; }

            ClassDeclarationSyntax? declaration = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (declaration == null) { return; }

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Make type partial",
                        ct => MakeTypePartial(context.Document, declaration, diagnostic, ct),
                        nameof(WinRTWrapperCodeFixer)),
                    diagnostic);
            }
        }

        private static async Task<Document> MakeTypePartial(Document document, ClassDeclarationSyntax @class, Diagnostic diagnostic, CancellationToken token)
        {
            SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (oldRoot == null) { return document; }

            ClassDeclarationSyntax newClass = @class.AddMembers(
                SyntaxFactory.MethodDeclaration(
                    default,
                    SyntaxTokenList.Create([
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.PartialKeyword)]),
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    default,
                    SyntaxFactory.Identifier("NewMethod"),
                    default,
                    SyntaxFactory.ParameterList(),
                    default, default, default, default));

            SyntaxNode newRoot = oldRoot.ReplaceNode(@class, newClass);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
