using System.Windows.Controls;
using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.WPF.Tests.SourceGenerator
{
    /// <summary>
    /// Test control with source-generated DependencyProperties and AspectInjector-transformed properties.
    /// </summary>
    public partial class TestControl : Control
    {
        #region Properties

        [StyledProperty(DefaultValue = "Default")]
        public string Text { get; set; } = default!;

        [StyledProperty(DefaultValue = 42, AffectsMeasure = true)]
        public int Number { get; set; }

        [StyledProperty(BindsTwoWayByDefault = true)]
        public bool IsChecked { get; set; }

        #endregion
    }
}
