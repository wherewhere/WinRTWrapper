using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;
using WinRTWrapper.SourceGenerators.Models;

namespace WinRTWrapper.SourceGenerators
{
    public partial class WinRTWrapperGenerator
    {
        /// <summary>
        /// Initializes the <see cref="StringBuilder"/> for the generated wrapper class.
        /// </summary>
        /// <param name="source">The source wrapper type.</param>
        /// <returns>The initialized <see cref="StringBuilder"/>.</returns>
        private static StringBuilder InitBuilder(in (INamedTypeSymbol, INamedTypeSymbol) source)
        {
            StringBuilder builder = new();
            (INamedTypeSymbol symbol, INamedTypeSymbol target) = source;
            if (!target.IsStatic)
            {
                return builder.AppendLine(
                    $$"""
                            /// <summary>
                            /// The target <see cref="{{target.GetDocumentationCommentId()}}"/> object of the wrapper.
                            /// </summary>
                            private readonly {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} target;

                            /// <summary>
                            /// Initializes a new instance of the <see cref="{{symbol.GetDocumentationCommentId()}}"/> class with the specified target <see cref="{{target.GetDocumentationCommentId()}}"/> object.
                            /// </summary>
                            /// <param name="target">The target <see cref="{{target.GetDocumentationCommentId()}}"/> object.</param>
                            internal {{symbol.Name}}({{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} target)
                            {
                                this.target = target;
                            }

                    """);
            }
            return builder;
        }

        /// <summary>
        /// Adds a <paramref name="method"/> to the generated wrapper class.
        /// </summary>
        /// <param name="source">The source wrapper type.</param>
        /// <param name="method">The method symbol to add.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the method code to.</param>
        /// <returns>The updated <see cref="StringBuilder"/> with the method code added.</returns>
        private static StringBuilder AddMethod(in (INamedTypeSymbol, INamedTypeSymbol) source, IMethodSymbol method, StringBuilder builder, ref bool? needConstructor)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target) = source;
            switch (method)
            {
                case { MethodKind: MethodKind.Constructor }:
                    _ = builder.AppendLine(
                        $$"""
                                /// <inheritdoc cref="{{method.GetDocumentationCommentId()}}"/>
                                public {{symbol.Name}}({{string.Join(" ", method.Parameters.Select(x => x.ToDisplayString()))}})
                                {
                                    this.target = new {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}({{string.Join(", ", method.Parameters.Select(x => x.Name))}});
                                }

                        """);
                    if (method.Parameters.Length == 0)
                    {
                        needConstructor = false;
                    }
                    else
                    {
                        needConstructor ??= true;
                    }
                    break;
                case { MethodKind: MethodKind.Ordinary }:
                    _ = builder.AppendLine(
                        $$"""
                                /// <inheritdoc cref="{{method.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(method)}}{{method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{method.Name}}({{string.Join(" ", method.Parameters.Select(x => x.ToDisplayString()))}})
                                {
                                    {{(method.ReturnsVoid ? string.Empty : "return ")}}{{GetMemberTarget(target, method)}}.{{method.Name}}({{string.Join(", ", method.Parameters.Select(x => x.Name))}});
                                }

                        """);
                    break;
            }
            return builder;
        }

        /// <summary>
        /// Adds a <paramref name="property"/> to the generated wrapper class.
        /// </summary>
        /// <param name="source">The source wrapper type.</param>
        /// <param name="property">The property symbol to add.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the property code to.</param>
        /// <returns>The updated <see cref="StringBuilder"/> with the property code added.</returns>
        private static StringBuilder AddProperty(in (INamedTypeSymbol, INamedTypeSymbol) source, IPropertySymbol property, StringBuilder builder)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target) = source;
            switch (property)
            {
                case { IsWriteOnly: true }:
                    return builder;
                case { IsIndexer: true }:
                    if (property is { IsStatic: false, Parameters.Length: 1 } && symbol.AllInterfaces.Any(x =>
                        x.IsGenericType
                        && x.ContainingNamespace.ToDisplayString() == "System.Collections.Generic"
                        && ((x.TypeArguments.Length == 1
                            && property.Parameters[0].Type.SpecialType == SpecialType.System_Int32
                            && (x.MetadataName, property.IsReadOnly) is ("IList`1", false) or ("IReadOnlyList`1", true)
                            && x.TypeArguments[0].Equals(property.Type, SymbolEqualityComparer.Default))
                        || (x.TypeArguments.Length == 2
                            && (x.MetadataName, property.IsReadOnly) is ("IDictionary`2", false) or ("IReadOnlyDictionary`2", true)
                            && x.TypeArguments[0].Equals(property.Parameters[0].Type, SymbolEqualityComparer.Default)
                            && x.TypeArguments[1].Equals(property.Type, SymbolEqualityComparer.Default)))))
                    {
                        _ = builder.AppendLine(
                            $$"""
                                    /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                    public {{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} this[{{string.Join(", ", property.Parameters.Select(x => x.ToDisplayString()))}}]
                                    {
                                        get
                                        {
                                            return this.target[{{string.Join(", ", property.Parameters.Select(x => x.Name))}}];
                                        }
                            """);
                        if (!property.IsReadOnly)
                        {
                            _ = builder.AppendLine(
                                $$"""

                                            set
                                            {
                                                this.target[{{string.Join(", ", property.Parameters.Select(x => x.Name))}}] = value;
                                            }
                                """);
                        }
                        _ = builder.AppendLine(
                            """
                                    }

                            """);
                        break;
                    }
                    return builder;
                default:
                    _ = builder.AppendLine(
                        $$"""
                                /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(property)}}{{property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{property.Name}}
                                {
                                    get
                                    {
                                        return {{GetMemberTarget(target, property)}}.{{property.Name}};
                                    }
                        """);
                    if (!property.IsReadOnly)
                    {
                        _ = builder.AppendLine(
                            $$"""
                            
                                        set
                                        {
                                            {{GetMemberTarget(target, property)}}.{{property.Name}} = value;
                                        }
                            """);
                    }
                    _ = builder.AppendLine(
                        """
                                }

                        """);
                    break;
            }
            return builder;
        }

        /// <summary>
        /// Adds an <paramref name="event"/> to the generated wrapper class.
        /// </summary>
        /// <param name="source">The source wrapper type.</param>
        /// <param name="event">The event symbol to add.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> to append the event code to.</param>
        /// <returns>The updated <see cref="StringBuilder"/> with the event code added.</returns>
        private static object AddEvent(INamedTypeSymbol target, IEventSymbol @event, StringBuilder builder, GenerationOptions options)
        {
            IMethodSymbol invoke = @event.Type.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == "Invoke");
            switch (options)
            {
                case { IsWinMDObject: true }:
                    _ = builder.AppendLine(
                        $$"""
                                private {{(@event.IsStatic ? "static " : string.Empty)}}bool _is_{{@event.Name}}_EventRegistered = false;
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventTable = new global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(@event)}}event {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
                                {
                                    add
                                    {
                                        if (!_is_{{@event.Name}}_EventRegistered)
                                        {
                                            {{GetMemberTarget(target, @event)}}.{{@event.Name}} += delegate ({{string.Join(", ", invoke.Parameters.Select(x => x.ToDisplayString()))}}) 
                                            {
                                                {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} @event = _{{@event.Name}}_EventTable.InvocationList;
                                                if (@event != null)
                                                {
                                                    {{(invoke.ReturnsVoid ? string.Empty : "return ")}}@event.Invoke({{string.Join(", ", invoke.Parameters.Select(x => x.Name))}});
                                                }
                                            };
                                            _is_{{@event.Name}}_EventRegistered = true;
                                        }
                                        return _{{@event.Name}}_EventTable.AddEventHandler(value);
                                    }
                                    remove
                                    {
                                        _{{@event.Name}}_EventTable.RemoveEventHandler(value);
                                    }
                                }

                        """);
                    break;
                default:
                    _ = builder.AppendLine(
                        $$"""
                                /// <inheritdoc cref="{{@event.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(@event)}}event {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
                                {
                                    add
                                    {
                                        {{GetMemberTarget(target, @event)}}.{{@event.Name}} += value;
                                    }
                                    remove
                                    {
                                        {{GetMemberTarget(target, @event)}}.{{@event.Name}} -= value;
                                    }
                                }

                        """);
                    break;
            }
            return builder;
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> member's modification string based on whether it is static or instance.
        /// </summary>
        /// <typeparam name="T">The type of the member.</typeparam>
        /// <param name="member">The member symbol.</param>
        /// <returns>The member's modification string.</returns>
        private static string GetMemberModify<T>(T member) where T : ISymbol =>
            $"public {(member.IsStatic ? "static " : string.Empty)}";

        /// <summary>
        /// Gets the target <typeparamref name="T"/> member's name based on whether it is static or instance.
        /// </summary>
        /// <typeparam name="T">The type of the member.</typeparam>
        /// <param name="target">The target type symbol.</param>
        /// <param name="member">The member symbol.</param>
        /// <returns>The member's target name.</returns>
        private static string GetMemberTarget<T>(INamedTypeSymbol target, T member) where T : ISymbol =>
            member.IsStatic ? target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : $"this.{nameof(target)}";
    }
}
