using System.Collections.Generic;
using Avalonia;
using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.Avalonia.Tests.SourceGenerator
{
    /// <summary>
    /// Attached properties with callbacks. Theirs must be static: the object a change fires
    /// for is not an instance of the class that declares the property.
    /// </summary>
    public static partial class TestCallbackAttachedProperties
    {
        #region Fields

        public static readonly List<string> Changes = new();

        #endregion

        #region Event Handlers

        private static void OnIsPinnedChanged(AvaloniaObject sender, AvaloniaPropertyChangedEventArgs e)
        {
            Changes.Add($"{sender.GetType().Name}:{e.NewValue}");
        }

        #endregion

        #region Properties

        [AttachedProperty(DefaultValue = false)]
        public static bool IsPinned { get; set; }

        #endregion
    }
}
