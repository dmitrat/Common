using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.WPF.Sample.Controls
{
    /// <summary>
    /// Demo control showing source-generated DependencyProperties.
    /// Properties marked with [StyledProperty] automatically get DependencyProperty infrastructure.
    /// </summary>
    public partial class DemoControl : UserControl
    {
        #region Fields

        private Border? m_border;
        private TextBlock? m_titleBlock;
        private TextBlock? m_counterBlock;

        #endregion

        #region Constructors

        public DemoControl()
        {
            InitializeVisual();
        }

        #endregion

        #region Initialization

        private void InitializeVisual()
        {
            m_border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Background = Brushes.White
            };

            var stack = new StackPanel();

            m_titleBlock = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold
            };
            m_titleBlock.SetBinding(TextBlock.TextProperty, 
                new System.Windows.Data.Binding(nameof(Title)) { Source = this });

            m_counterBlock = new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0)
            };
            m_counterBlock.SetBinding(TextBlock.TextProperty,
                new System.Windows.Data.Binding(nameof(Counter)) { Source = this, StringFormat = "Counter: {0}" });

            stack.Children.Add(m_titleBlock);
            stack.Children.Add(m_counterBlock);

            m_border.Child = stack;
            Content = m_border;
        }

        #endregion

        #region Functions

        private void UpdateHighlight(bool isHighlighted)
        {
            if (m_border != null)
            {
                m_border.Background = isHighlighted 
                    ? new SolidColorBrush(Color.FromRgb(255, 255, 200)) 
                    : Brushes.White;
                m_border.BorderBrush = isHighlighted ? Brushes.Orange : Brushes.Gray;
                m_border.BorderThickness = new Thickness(isHighlighted ? 2 : 1);
            }
        }

        /// <summary>
        /// Convention-based callback - automatically discovered by source generator.
        /// </summary>
        private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoControl control)
            {
                control.UpdateHighlight((bool)e.NewValue);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Title property - demonstrates basic StyledProperty with default value.
        /// The source generator creates: public static readonly DependencyProperty TitleProperty
        /// </summary>
        [StyledProperty(DefaultValue = "Untitled")]
        public string Title { get; set; } = default!;

        /// <summary>
        /// Counter property - demonstrates StyledProperty with AffectsRender.
        /// Changes to this property trigger a re-render.
        /// </summary>
        [StyledProperty(DefaultValue = 0, AffectsRender = true)]
        public int Counter { get; set; }

        /// <summary>
        /// IsHighlighted property - demonstrates two-way binding by default.
        /// </summary>
        [StyledProperty(DefaultValue = false, BindsTwoWayByDefault = true)]
        public bool IsHighlighted { get; set; }

        #endregion
    }
}
