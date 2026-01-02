using Avalonia.Controls;
using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.Avalonia.Tests.SourceGenerator
{
    /// <summary>
    /// Test control with source-generated DirectProperties.
    /// </summary>
    public partial class TestDirectPropertyControl : Control
    {
        #region Properties

        [DirectProperty(DefaultValue = 0)]
        public int Counter { get; set; }

        [DirectProperty(DefaultValue = "")]
        public string Label { get; set; } = default!;

        [DirectProperty(BindsTwoWayByDefault = true)]
        public bool IsActive { get; set; }

        #endregion
    }
}
