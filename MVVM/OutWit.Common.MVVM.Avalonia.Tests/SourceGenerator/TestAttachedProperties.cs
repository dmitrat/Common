using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.Avalonia.Tests.SourceGenerator
{
    /// <summary>
    /// Test class with source-generated attached AvaloniaProperties.
    /// </summary>
    public static partial class TestAttachedProperties
    {
        #region Properties

        [AttachedProperty(DefaultValue = false)]
        public static bool IsHighlighted { get; set; }

        [AttachedProperty(DefaultValue = 0.0)]
        public static double Opacity { get; set; }

        [AttachedProperty(DefaultValue = "")]
        public static string Tag { get; set; } = default!;

        #endregion
    }
}
