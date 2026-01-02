using System;
using AspectInjector.Broker;
using OutWit.Common.MVVM.Avalonia.Aspects;

namespace OutWit.Common.MVVM.Attributes
{
    /// <summary>
    /// Marks a property for automatic Avalonia AttachedProperty generation.
    /// 
    /// Usage:
    /// 1. Mark your static property with [AttachedProperty]
    /// 2. The source generator creates the {PropertyName}Property field and Get/Set methods
    /// 
    /// Example:
    /// <code>
    /// public static partial class MyAttachedProperties
    /// {
    ///     [AttachedProperty(DefaultValue = false)]
    ///     public static bool IsHighlighted { get; set; }
    /// }
    /// 
    /// // Generated:
    /// // public static readonly AttachedProperty&lt;bool&gt; IsHighlightedProperty = ...
    /// // public static bool GetIsHighlighted(AvaloniaObject obj) => ...
    /// // public static void SetIsHighlighted(AvaloniaObject obj, bool value) => ...
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    [Injection(typeof(AttachedPropertyAspect))]
    public sealed class AttachedPropertyAttribute : Attribute
    {
        #region Constructors

        public AttachedPropertyAttribute()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the generated AttachedProperty field.
        /// Default: {PropertyName}Property
        /// </summary>
        public string? PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the default value for the property.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets whether the property value is inherited by child elements.
        /// </summary>
        public bool Inherits { get; set; }

        /// <summary>
        /// Gets or sets the name of the PropertyChangedCallback method.
        /// If not specified, the generator will look for a method named On{PropertyName}Changed.
        /// </summary>
        public string? OnChanged { get; set; }

        /// <summary>
        /// Gets or sets the name of the CoerceValueCallback method.
        /// If not specified, the generator will look for a method named {PropertyName}Coerce.
        /// </summary>
        public string? Coerce { get; set; }

        #endregion
    }
}
