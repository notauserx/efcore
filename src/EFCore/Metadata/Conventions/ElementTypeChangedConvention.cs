﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     A convention that reacts to changes made to element types of primitive collections.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-conventions">Model building conventions</see> for more information and examples.
/// </remarks>
public class ElementTypeChangedConvention :
    IPropertyElementTypeChangedConvention, IForeignKeyAddedConvention, IForeignKeyPropertiesChangedConvention
{
    internal static readonly bool UseOldBehavior32411 =
        AppContext.TryGetSwitch("Microsoft.EntityFrameworkCore.Issue32411", out var enabled32411) && enabled32411;

    internal static readonly bool UseOldBehavior33704 =
        AppContext.TryGetSwitch("Microsoft.EntityFrameworkCore.Issue33704", out var enabled33704) && enabled33704;

    /// <summary>
    ///     Creates a new instance of <see cref="ElementTypeChangedConvention" />.
    /// </summary>
    /// <param name="dependencies">Parameter object containing dependencies for this convention.</param>
    public ElementTypeChangedConvention(ProviderConventionSetBuilderDependencies dependencies)
    {
        Dependencies = dependencies;
    }

    /// <summary>
    ///     Dependencies for this service.
    /// </summary>
    protected virtual ProviderConventionSetBuilderDependencies Dependencies { get; }

    /// <inheritdoc />
    public void ProcessPropertyElementTypeChanged(
        IConventionPropertyBuilder propertyBuilder,
        IElementType? newElementType,
        IElementType? oldElementType,
        IConventionContext<IElementType> context)
    {
        var keyProperty = propertyBuilder.Metadata;
        foreach (var key in keyProperty.GetContainingKeys())
        {
            var index = key.Properties.IndexOf(keyProperty);
            foreach (var foreignKey in key.GetReferencingForeignKeys())
            {
                var foreignKeyProperty = foreignKey.Properties[index];
                foreignKeyProperty.Builder.SetElementType(newElementType?.ClrType);
            }
        }
    }

    /// <inheritdoc />
    public void ProcessForeignKeyAdded(
        IConventionForeignKeyBuilder foreignKeyBuilder, IConventionContext<IConventionForeignKeyBuilder> context)
        => ProcessForeignKey(foreignKeyBuilder);

    /// <inheritdoc />
    public void ProcessForeignKeyPropertiesChanged(
        IConventionForeignKeyBuilder relationshipBuilder,
        IReadOnlyList<IConventionProperty> oldDependentProperties,
        IConventionKey oldPrincipalKey,
        IConventionContext<IReadOnlyList<IConventionProperty>> context)
    {
        if (relationshipBuilder.Metadata.IsInModel
            && !UseOldBehavior33704)
        {
            ProcessForeignKey(relationshipBuilder);
        }
    }

    private static void ProcessForeignKey(IConventionForeignKeyBuilder foreignKeyBuilder)
    {
        var foreignKeyProperties = foreignKeyBuilder.Metadata.Properties;
        var principalKeyProperties = foreignKeyBuilder.Metadata.PrincipalKey.Properties;
        for (var i = 0; i < foreignKeyProperties.Count; i++)
        {
            foreignKeyProperties[i].Builder.SetElementType(principalKeyProperties[i].GetElementType()?.ClrType);
        }
    }
}
