using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.Avalonia.Sample.Controls;

/// <summary>
/// Demo control showing source-generated StyledProperties.
/// Properties marked with [StyledProperty] automatically get property infrastructure.
/// </summary>
public partial class DemoStyledControl : UserControl
{
    #region Fields

    private Border? m_border;
    private TextBlock? m_titleBlock;
    private TextBlock? m_counterBlock;

    #endregion

    #region Constructors

    public DemoStyledControl()
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

        var stack = new StackPanel { Spacing = 4 };

        m_titleBlock = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            [!TextBlock.TextProperty] = this[!TitleProperty]
        };

        m_counterBlock = new TextBlock();
        // Bind to Counter with format
        this.GetObservable(CounterProperty).Subscribe(value =>
        {
            m_counterBlock.Text = $"Counter: {value}";
        });

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
    private void OnIsHighlightedChanged(AvaloniaPropertyChangedEventArgs<bool> e)
    {
        UpdateHighlight(e.NewValue.Value);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Title property - demonstrates basic StyledProperty with default value.
    /// </summary>
    [StyledProperty(DefaultValue = "Untitled")]
    public string Title { get; set; } = default!;

    /// <summary>
    /// Counter property - demonstrates StyledProperty.
    /// </summary>
    [StyledProperty(DefaultValue = 0)]
    public int Counter { get; set; }

    /// <summary>
    /// IsHighlighted property - demonstrates two-way binding by default.
    /// </summary>
    [StyledProperty(DefaultValue = false, BindsTwoWayByDefault = true)]
    public bool IsHighlighted { get; set; }

    #endregion
}
