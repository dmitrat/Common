using System;
using AspectInjector.Broker;
using OutWit.Common.MVVM.Avalonia.Aspects;

namespace OutWit.Common.MVVM.Attributes
{
    /// <summary>
    /// Marks a property for automatic Avalonia StyledProperty generation.
    /// 
    /// Usage:
    /// 1. Mark your property with [StyledProperty]
    /// 2. The source generator creates the {PropertyName}Property field
    /// 3. AspectInjector transforms getter/setter to use GetValue/SetValue
    /// 
    /// Example:
    /// <code>
    /// public partial class MyControl : Control
    /// {
    ///     [StyledProperty(DefaultValue = "Hello")]
    ///     public string Text { get; set; }
    /// }
    /// 
    /// // Generated: public static readonly StyledProperty&lt;string&gt; TextProperty = ...
    /// // Transformed: get => GetValue(TextProperty); set => SetValue(TextProperty, value);
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    [Injection(typeof(StyledPropertyAspect))]
    public sealed class StyledPropertyAttribute : Attribute
    {
        #region Constructors

        public StyledPropertyAttribute()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the generated StyledProperty field.
        /// Default: {PropertyName}Property
        /// </summary>
        public string? PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the default value for the property.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets whether the property binds two-way by default.
        /// </summary>
        public bool BindsTwoWayByDefault { get; set; }

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
