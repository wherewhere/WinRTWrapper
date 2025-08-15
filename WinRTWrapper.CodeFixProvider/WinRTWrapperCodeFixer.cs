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
                if (diagnostic.Properties.TryGetValue("Name", out string? name)
                    && diagnostic.Properties.TryGetValue("Definition", out string? definition))
                {
                    string title = $"Add member {name} to {declaration.Identifier.Text}";
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title,
                            ct => AddMember(context.Document, declaration, definition, ct),
                            title),
                        diagnostic);
                }
            }
        }

        private static async Task<Document> AddMember(Document document, ClassDeclarationSyntax @class, string? definition, CancellationToken token)
        {
            SyntaxNode? oldRoot = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
            if (oldRoot == null) { return document; }
            if (definition != null && SyntaxFactory.ParseMemberDeclaration(definition) is MemberDeclarationSyntax syntax)
            {
                ClassDeclarationSyntax newClass = @class.AddMembers(syntax);
                SyntaxNode newRoot = oldRoot.ReplaceNode(@class, newClass);
                return document.WithSyntaxRoot(newRoot);
            }
            return document;
        }
    }
}
