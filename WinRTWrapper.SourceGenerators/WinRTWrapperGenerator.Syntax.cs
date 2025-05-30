using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
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
                _ = builder.AppendLine(
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
                foreach (AttributeData attribute in symbol.GetAttributes().Where(x =>
                    x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                    && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName))
                {
                    if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managedType }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapperType }])
                    {
                        _ = builder.AppendLine(
                            $$"""
                                    /// <summary>
                                    /// Converts a managed type <see cref="{{managedType.GetDocumentationCommentId()}}"/> to a wrapper type <see cref={{wrapperType.GetDocumentationCommentId()}}"/>.
                                    /// </summary>
                                    internal static {{wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} ConvertToWrapper({{managedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} managed)
                                    {
                                        return ({{wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}})new {{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(managed);
                                    }

                                    /// <summary>
                                    /// Converts a wrapper type <see cref={{wrapperType.GetDocumentationCommentId()}}"/> to a managed type <see cref={{managedType.GetDocumentationCommentId()}}"/>.
                                    /// </summary>
                                    internal static {{managedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} ConvertToManaged({{wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} wrapper)
                                    {
                                        return ({{managedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}})(({{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}})wrapper).target;
                                    }

                            """);
                    }
                }
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
                    MarshalType returnType = GetWrapperType(method.GetReturnTypeAttributes(), method.ReturnType);
                    ImmutableArray<(MarshalType marshal, string name)> parameters = [.. method.Parameters.Select(static x => (GetWrapperType(x.GetAttributes(), x.Type), x.Name))];
                    _ = builder.AppendLine(
                        $$"""
                                /// <inheritdoc cref="{{method.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(method)}}{{returnType.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{method.Name}}({{string.Join(" ", parameters.Select(x => $"{x.marshal.Type.ToDisplayString()} {x.name}"))}})
                                {
                                    {{(method.ReturnsVoid ? string.Empty : "return ")}}{{ConvertToWrapper(returnType.Marshaller, $"{GetMemberTarget(target, method)}.{method.Name}({string.Join(", ", parameters.Select(x => ConvertToManaged(x.marshal.Marshaller, x.name)))})")}};
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
                        MarshalType returnType = GetWrapperType(property.GetAttributes(), property.Type);
                        ImmutableArray<(MarshalType marshal, string name)> parameters = [.. property.Parameters.Select(static x => (GetWrapperType(x.GetAttributes(), x.Type), x.Name))];
                        _ = builder.AppendLine(
                            $$"""
                                    /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                    public {{returnType.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} this[{{string.Join(" ", parameters.Select(x => $"{x.marshal.Type.ToDisplayString()} {x.name}"))}}]
                                    {
                                        get
                                        {
                                            return this.target[{{string.Join(", ", parameters.Select(x => ConvertToManaged(x.marshal.Marshaller, x.name)))}}];
                                        }
                            """);
                        if (!property.IsReadOnly)
                        {
                            _ = builder.AppendLine(
                                $$"""
                                            set
                                            {
                                                this.target[{{string.Join(", ", parameters.Select(x => ConvertToManaged(x.marshal.Marshaller, x.name)))}}] = {{ConvertToManaged(returnType.Marshaller, "value")}};
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
                    MarshalType marshal = GetWrapperType(property.GetAttributes(), property.Type);
                    _ = builder.AppendLine(
                        $$"""
                                /// <inheritdoc cref="{{property.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(property)}}{{marshal.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{property.Name}}
                                {
                                    get
                                    {
                                        return {{ConvertToWrapper(marshal.Marshaller, $"{GetMemberTarget(target, property)}.{property.Name}")}};
                                    }
                        """);
                    if (!property.IsReadOnly)
                    {
                        _ = builder.AppendLine(
                            $$"""
                            
                                        set
                                        {
                                            {{GetMemberTarget(target, property)}}.{{property.Name}} = {{ConvertToManaged(marshal.Marshaller, "value")}};
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
        private static object AddEvent(in (INamedTypeSymbol, INamedTypeSymbol) source, IEventSymbol @event, StringBuilder builder, GenerationOptions options)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target) = source;
            IMethodSymbol invoke = @event.Type.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(x => x.Name == "Invoke");
            MarshalType marshal = GetWrapperType(@event.GetAttributes(), @event.Type);
            switch ((options, marshal))
            {
                case ({ IsWinMDObject: true }, { Marshaller: INamedTypeSymbol marshaller }):
                    _ = builder.AppendLine(
                        $$"""
                                /// <summary>
                                /// The singleton flag for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event registration.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}bool _is_{{@event.Name}}_EventRegistered = false;
                                /// <summary>
                                /// The event registration token table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventTable = new global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(@event)}}event {{marshal.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
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
                                                return{{(invoke.ReturnsVoid ? string.Empty : $" default({invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})")}};
                                            };
                                            _is_{{@event.Name}}_EventRegistered = true;
                                        }
                                        return _{{@event.Name}}_EventTable.AddEventHandler({{ConvertToManaged(marshaller, "value")}});
                                    }
                                    remove
                                    {
                                        _{{@event.Name}}_EventTable.RemoveEventHandler(value);
                                    }
                                }

                        """);
                    break;
                case ({ IsWinMDObject: false }, { Marshaller: INamedTypeSymbol marshaller }):
                    _ = builder.AppendLine(
                        $$"""
                                /// <summary>
                                /// The event weak table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<{{marshal.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}, {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventWeakTable = new global::System.Runtime.CompilerServices.ConditionalWeakTable<{{marshal.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}, {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetDocumentationCommentId()}}"/>
                                {{GetMemberModify(@event)}}event {{marshal.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
                                {
                                    add
                                    {
                                        {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} handle = {{ConvertToManaged(marshaller, "value")}};
                                        {{GetMemberTarget(target, @event)}}.{{@event.Name}} += handle;
                                        _{{@event.Name}}_EventWeakTable.Add(value, handle);
                                    }
                                    remove
                                    {
                                        {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} handle;
                                        if (_{{@event.Name}}_EventWeakTable.TryGetValue(value, out handle))
                                        {
                                            {{GetMemberTarget(target, @event)}}.{{@event.Name}} -= handle;
                                            _{{@event.Name}}_EventWeakTable.Remove(value);
                                        }
                                    }
                                }

                        """);
                    break;
                case ({ IsWinMDObject: true }, { Marshaller: null }):
                    _ = builder.AppendLine(
                        $$"""
                                /// <summary>
                                /// The singleton flag for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event registration.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}bool _is_{{@event.Name}}_EventRegistered = false;
                                /// <summary>
                                /// The event registration token table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
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
                                                return{{(invoke.ReturnsVoid ? string.Empty : $" default({invoke.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})")}};
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
                case ({ IsWinMDObject: false }, { Marshaller: null }):
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

        /// <summary>
        /// Gets the wrapper type for a given <paramref name="original"/> type based on its attributes.
        /// </summary>
        /// <param name="attributes">The attributes of the type.</param>
        /// <param name="original">The original type symbol.</param>
        /// <returns>The wrapper type and its marshaller if applicable.</returns>
        private static MarshalType GetWrapperType(ImmutableArray<AttributeData> attributes, ITypeSymbol original)
        {
            if (attributes.FirstOrDefault(x =>
                x.AttributeClass is { Name: nameof(WinRTWrapperMarshalUsingAttribute) }
                && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol marshaller }] })
            {
                if (marshaller.GetAttributes().FirstOrDefault(x =>
                    x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                    && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                    is { ConstructorArguments: [_, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] })
                {
                    return new MarshalType(wrapper, marshaller);
                }
            }
            return new MarshalType(original, null);
        }

        /// <summary>
        /// Converts the given <paramref name="inner"/> string to a wrapper type using the specified <paramref name="marshaller"/>.
        /// </summary>
        /// <param name="marshaller">The marshaller type symbol.</param>
        /// <param name="inner">The inner string to convert.</param>
        /// <returns>The converted string in the wrapper type.</returns>
        private static string ConvertToWrapper(INamedTypeSymbol? marshaller, string inner)
        {
            if (marshaller != null && marshaller.GetAttributes().FirstOrDefault(x =>
                x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managed }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] })
            {
                return $"({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{nameof(ConvertToWrapper)}(({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))";
            }
            return inner;
        }

        /// <summary>
        /// Converts the given <paramref name="inner"/> string to a managed type using the specified <paramref name="marshaller"/>.
        /// </summary>
        /// <param name="marshaller">The marshaller type symbol.</param>
        /// <param name="inner">The inner string to convert.</param>
        /// <returns>The converted string in the managed type.</returns>
        private static string ConvertToManaged(INamedTypeSymbol? marshaller, string inner)
        {
            if (marshaller != null && marshaller.GetAttributes().FirstOrDefault(x =>
                x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managed }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] })
            {
                return $"({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{nameof(ConvertToManaged)}(({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))";
            }
            return inner;
        }

        /// <summary>
        /// Represents a type and its associated marshaller, if any, for use in interop scenarios.
        /// </summary>
        /// <param name="Type">The type symbol.</param>
        /// <param name="Marshaller">The marshaller type symbol, if applicable.</param>
        private readonly record struct MarshalType(ITypeSymbol Type, INamedTypeSymbol? Marshaller);
    }
}
