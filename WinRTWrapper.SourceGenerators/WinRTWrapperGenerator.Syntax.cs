using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using WinRTWrapper.SourceGenerators.Extensions;
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
                _ = builder.AppendLine(handler:
                    $$"""
                            /// <summary>
                            /// The target <see cref="{{target.GetConstructedFromDocumentationCommentId()}}"/> object of the wrapper.
                            /// </summary>
                            private readonly {{target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} target;

                            /// <summary>
                            /// Initializes a new instance of the <see cref="{{symbol.GetConstructedFromDocumentationCommentId()}}"/> class with the specified target <see cref="{{target.GetConstructedFromDocumentationCommentId()}}"/> object.
                            /// </summary>
                            /// <param name="target">The target <see cref="{{target.GetConstructedFromDocumentationCommentId()}}"/> object.</param>
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
                        _ = builder.AppendLine(handler:
                            $$"""
                                    /// <summary>
                                    /// Converts a managed type <see cref="{{managedType.GetConstructedFromDocumentationCommentId()}}"/> to a wrapper type <see cref="{{wrapperType.GetConstructedFromDocumentationCommentId()}}"/>.
                                    /// </summary>
                                    /// <param name="managed">The managed type to convert.</param>
                                    /// <returns>The converted wrapper type.</returns>
                                    internal static {{wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} ConvertToWrapper({{managedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} managed)
                                    {
                                        return ({{wrapperType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}})new {{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}(managed);
                                    }

                                    /// <summary>
                                    /// Converts a wrapper type <see cref="{{wrapperType.GetConstructedFromDocumentationCommentId()}}"/> to a managed type <see cref="{{managedType.GetConstructedFromDocumentationCommentId()}}"/>.
                                    /// </summary>
                                    /// <param name="wrapper">The wrapper type to convert.</param>
                                    /// <returns>The converted managed type.</returns>
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
        private static StringBuilder AddMethod(in (INamedTypeSymbol, INamedTypeSymbol) source, IMethodSymbol method, StringBuilder builder, ImmutableArray<MarshalType> marshals, ref bool? needConstructor)
        {
            (INamedTypeSymbol symbol, INamedTypeSymbol target) = source;
            switch (method)
            {
                case { MethodKind: MethodKind.Constructor }:
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <inheritdoc cref="{{method.GetConstructedFromDocumentationCommentId()}}"/>
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
                    MarshalType returnType = GetWrapperType(method.GetReturnTypeAttributes(), marshals, method.ReturnType);
                    ImmutableArray<(MarshalType marshal, string name)> parameters = [.. method.Parameters.Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type), x.Name))];
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <inheritdoc cref="{{method.GetConstructedFromDocumentationCommentId()}}"/>
                                {{method.GetMemberModify()}}{{returnType.WrapperTypeName}} {{method.Name}}({{string.Join(" ", parameters.Select(x => $"{x.marshal.WrapperTypeName} {x.name}"))}})
                                {
                                    {{(method.ReturnsVoid ? string.Empty : "return ")}}{{returnType.ConvertToWrapper($"{target.GetMemberTarget(method)}.{method.Name}({string.Join(", ", parameters.Select(x => x.marshal.ConvertToManaged(x.name)))})")}};
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
        private static StringBuilder AddProperty(in (INamedTypeSymbol, INamedTypeSymbol) source, IPropertySymbol property, StringBuilder builder, ImmutableArray<MarshalType> marshals)
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
                        MarshalType returnType = GetWrapperType(property.GetAttributes(), marshals, property.Type);
                        ImmutableArray<(MarshalType marshal, string name)> parameters = [.. property.Parameters.Select(x => (GetWrapperType(x.GetAttributes(), marshals, x.Type), x.Name))];
                        _ = builder.AppendLine(handler:
                            $$"""
                                    /// <inheritdoc cref="{{property.GetConstructedFromDocumentationCommentId()}}"/>
                                    public {{returnType.WrapperTypeName}} this[{{string.Join(" ", parameters.Select(x => $"{x.marshal.WrapperTypeName} {x.name}"))}}]
                                    {
                                        get
                                        {
                                            return this.target[{{string.Join(", ", parameters.Select(x => x.marshal.ConvertToManaged(x.name)))}}];
                                        }
                            """);
                        if (!property.IsReadOnly)
                        {
                            _ = builder.AppendLine(handler:
                                $$"""
                                            set
                                            {
                                                this.target[{{string.Join(", ", parameters.Select(x => x.marshal.ConvertToManaged(x.name)))}}] = {{returnType.ConvertToManaged("value")}};
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
                    MarshalType marshal = GetWrapperType(property.GetAttributes(), marshals, property.Type);
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <inheritdoc cref="{{property.GetConstructedFromDocumentationCommentId()}}"/>
                                {{property.GetMemberModify()}}{{marshal.WrapperTypeName}} {{property.Name}}
                                {
                                    get
                                    {
                                        return {{marshal.ConvertToWrapper($"{target.GetMemberTarget(property)}.{property.Name}")}};
                                    }
                        """);
                    if (!property.IsReadOnly)
                    {
                        _ = builder.AppendLine(handler:
                            $$"""
                            
                                        set
                                        {
                                            {{target.GetMemberTarget(property)}}.{{property.Name}} = {{marshal.ConvertToManaged("value")}};
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
            MarshalType marshal = GetWrapperType(@event.GetAttributes(), options.Marshals, @event.Type);
            switch ((options, marshal))
            {
                case ({ IsWinMDObject: true }, { HasConversion: true }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <summary>
                                /// The singleton flag for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event registration.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}bool _is_{{@event.Name}}_EventRegistered = false;
                                /// <summary>
                                /// The event registration token table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventTable = new global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{marshal.WrapperTypeName}} {{@event.Name}}
                                {
                                    add
                                    {
                                        if (!_is_{{@event.Name}}_EventRegistered)
                                        {
                                            {{target.GetMemberTarget(@event)}}.{{@event.Name}} += delegate ({{string.Join(", ", invoke.Parameters.Select(x => x.ToDisplayString()))}}) 
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
                                        return _{{@event.Name}}_EventTable.AddEventHandler({{marshal.ConvertToManaged("value")}});
                                    }
                                    remove
                                    {
                                        _{{@event.Name}}_EventTable.RemoveEventHandler(value);
                                    }
                                }

                        """);
                    break;
                case ({ IsWinMDObject: false }, { HasConversion: true }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <summary>
                                /// The event weak table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<{{marshal.WrapperTypeName}}, {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventWeakTable = new global::System.Runtime.CompilerServices.ConditionalWeakTable<{{marshal.WrapperTypeName}}, {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{marshal.WrapperTypeName}} {{@event.Name}}
                                {
                                    add
                                    {
                                        {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} handle = {{marshal.ConvertToManaged("value")}};
                                        {{target.GetMemberTarget(@event)}}.{{@event.Name}} += handle;
                                        _{{@event.Name}}_EventWeakTable.Add(value, handle);
                                    }
                                    remove
                                    {
                                        {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} handle;
                                        if (_{{@event.Name}}_EventWeakTable.TryGetValue(value, out handle))
                                        {
                                            {{target.GetMemberTarget(@event)}}.{{@event.Name}} -= handle;
                                            _{{@event.Name}}_EventWeakTable.Remove(value);
                                        }
                                    }
                                }

                        """);
                    break;
                case ({ IsWinMDObject: true }, { HasConversion: false }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <summary>
                                /// The singleton flag for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event registration.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}bool _is_{{@event.Name}}_EventRegistered = false;
                                /// <summary>
                                /// The event registration token table for the <see cref="{{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.{{@event.Name}}"/> event.
                                /// </summary>
                                private {{(@event.IsStatic ? "static " : string.Empty)}}readonly global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}> _{{@event.Name}}_EventTable = new global::System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<{{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>();
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
                                {
                                    add
                                    {
                                        if (!_is_{{@event.Name}}_EventRegistered)
                                        {
                                            {{target.GetMemberTarget(@event)}}.{{@event.Name}} += delegate ({{string.Join(", ", invoke.Parameters.Select(x => x.ToDisplayString()))}}) 
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
                case ({ IsWinMDObject: false }, { HasConversion: false }):
                    _ = builder.AppendLine(handler:
                        $$"""
                                /// <inheritdoc cref="{{@event.GetConstructedFromDocumentationCommentId()}}"/>
                                {{@event.GetMemberModify()}}event {{@event.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}} {{@event.Name}}
                                {
                                    add
                                    {
                                        {{target.GetMemberTarget(@event)}}.{{@event.Name}} += value;
                                    }
                                    remove
                                    {
                                        {{target.GetMemberTarget(@event)}}.{{@event.Name}} -= value;
                                    }
                                }

                        """);
                    break;
            }
            return builder;
        }

        /// <summary>
        /// Gets the wrapper type for a given <paramref name="original"/> type based on its attributes.
        /// </summary>
        /// <param name="attributes">The attributes of the type.</param>
        /// <param name="original">The original type symbol.</param>
        /// <returns>The wrapper type and its marshaller if applicable.</returns>
        private static MarshalType GetWrapperType(ImmutableArray<AttributeData> attributes, ImmutableArray<MarshalType> marshals, ITypeSymbol original)
        {
            static bool GetWrapperType(ImmutableArray<AttributeData> attributes, string name, ITypeSymbol original, [NotNullWhen(true)] out MarshalType? marshal)
            {
                if (attributes.FirstOrDefault(x =>
                    x.AttributeClass?.Name == name
                    && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                    is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol marshaller }] })
                {
                    if (marshaller.GetAttributes().FirstOrDefault(x =>
                        x.AttributeClass is { Name: nameof(WinRTWrapperMarshallerAttribute) }
                        && x.AttributeClass.ContainingNamespace.ToDisplayString() == namespaceName)
                        is { ConstructorArguments: [{ Kind: TypedConstantKind.Type, Value: INamedTypeSymbol managed }, { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol wrapper }] }
                        && original.IsSubclassOf(managed))
                    {
                        marshal = new MarshalType(
                            managed,
                            wrapper,
                            inner => $"({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ConvertToWrapper(({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))",
                            inner => $"({managed.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){marshaller.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ConvertToManaged(({wrapper.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({inner}))");
                        return true;
                    }
                }
                marshal = default;
                return false;
            }

            static bool GetMarshalType(ImmutableArray<MarshalType> marshals, ITypeSymbol original, [NotNullWhen(true)] out MarshalType? marshal)
            {
                if (marshals.FirstOrDefault(x => original.IsSubclassOf(x.ManagedType)) is MarshalType marshier)
                {
                    if (marshier is MarshalGenericType generic && original is INamedTypeSymbol symbol)
                    {
                        generic.GenericArguments = symbol.TypeArguments;
                    }
                    marshal = marshier;
                    return true;
                }
                marshal = default;
                return false;
            }

            return GetWrapperType(attributes, nameof(WinRTWrapperMarshalUsingAttribute), original, out MarshalType? marshal) ? marshal
                : GetWrapperType(original.GetAttributes(), nameof(WinRTWrapperMarshallingAttribute), original, out marshal) ? marshal
                : GetMarshalType(marshals, original, out marshal) ? marshal
                : new MarshalType(original, original);
        }
    }
}
