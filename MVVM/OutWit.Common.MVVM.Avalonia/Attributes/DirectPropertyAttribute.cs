using System;
using AspectInjector.Broker;
using OutWit.Common.MVVM.Avalonia.Aspects;

namespace OutWit.Common.MVVM.Attributes
{
    /// <summary>
    /// Marks a property for automatic Avalonia DirectProperty generation.
    /// DirectProperty is optimized for performance and stores the value directly in the object.
    /// 
    /// Usage:
    /// 1. Mark your property with [DirectProperty]
    /// 2. The source generator creates the {PropertyName}Property field and backing field
    /// 3. AspectInjector transforms getter/setter to use the generated code
    /// 
    /// Example:
    /// <code>
    /// public partial class MyControl : Control
    /// {
    ///     [DirectProperty(DefaultValue = 0)]
    ///     public int Count { get; set; }
    /// }
    /// 
    /// // Generated: 
    /// // private int m_count = 0;
    /// // public static readonly DirectProperty&lt;MyControl, int&gt; CountProperty = ...
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    [Injection(typeof(DirectPropertyAspect))]
    public sealed class DirectPropertyAttribute : Attribute
    {
        #region Constructors

        public DirectPropertyAttribute()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the generated DirectProperty field.
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
        /// Gets or sets the name of the PropertyChangedCallback method.
        /// If not specified, the generator will look for a method named On{PropertyName}Changed.
        /// </summary>
        public string? OnChanged { get; set; }

        #endregion
    }
}
