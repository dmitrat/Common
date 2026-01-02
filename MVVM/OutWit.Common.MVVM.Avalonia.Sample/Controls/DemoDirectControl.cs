using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using OutWit.Common.MVVM.Attributes;

namespace OutWit.Common.MVVM.Avalonia.Sample.Controls;

/// <summary>
/// Demo control showing source-generated DirectProperties.
/// DirectProperty is optimized for frequently changing values.
/// Unlike StyledProperty, DirectProperty doesn't participate in the styling system.
/// </summary>
public partial class DemoDirectControl : UserControl
{
    #region Fields

    private Border? m_progressBorder;
    private TextBlock? m_statusBlock;

    #endregion

    #region Constructors

    public DemoDirectControl()
    {
        InitializeVisual();
    }

    #endregion

    #region Initialization

    private void InitializeVisual()
    {
        var mainBorder = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12),
            Background = Brushes.White
        };

        var stack = new StackPanel { Spacing = 8 };

        // Progress bar
        var progressContainer = new Border
        {
            Height = 20,
            Background = Brushes.LightGray,
            CornerRadius = new CornerRadius(4),
            ClipToBounds = true
        };

        m_progressBorder = new Border
        {
            Background = Brushes.DodgerBlue,
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left
        };

        progressContainer.Child = m_progressBorder;

        // Status text
        m_statusBlock = new TextBlock
        {
            FontStyle = FontStyle.Italic,
            Foreground = Brushes.Gray
        };

        stack.Children.Add(new TextBlock { Text = "Progress:", FontWeight = FontWeight.SemiBold });
        stack.Children.Add(progressContainer);
        stack.Children.Add(m_statusBlock);

        mainBorder.Child = stack;
        Content = mainBorder;

        // Subscribe to property changes
        this.GetObservable(ProgressProperty).Subscribe(UpdateProgress);
        this.GetObservable(StatusTextProperty).Subscribe(UpdateStatus);
    }

    #endregion

    #region Functions

    private void UpdateProgress(double value)
    {
        if (m_progressBorder != null)
        {
            var percentage = Math.Clamp(value, 0, 100) / 100.0;
            // Calculate width based on parent
            m_progressBorder.Width = 276 * percentage; // 300 - padding
        }
    }

    private void UpdateStatus(string? value)
    {
        if (m_statusBlock != null)
        {
            m_statusBlock.Text = string.IsNullOrEmpty(value) ? "No status" : value;
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Progress property - DirectProperty for frequently changing values.
    /// DirectProperty generates backing field and uses RegisterDirect.
    /// </summary>
    [DirectProperty(DefaultValue = 0.0)]
    public double Progress { get; set; }

    /// <summary>
    /// StatusText property - DirectProperty with two-way binding.
    /// </summary>
    [DirectProperty(DefaultValue = "", BindsTwoWayByDefault = true)]
    public string StatusText { get; set; } = default!;

    #endregion
}
