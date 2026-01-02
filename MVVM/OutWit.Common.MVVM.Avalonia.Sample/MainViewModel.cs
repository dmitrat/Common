using System.ComponentModel;
using System.Runtime.CompilerServices;
using OutWit.Common.MVVM.Commands;

namespace OutWit.Common.MVVM.Avalonia.Sample;

/// <summary>
/// Main ViewModel demonstrating RelayCommand usage.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Fields

    private string m_controlTitle = "Demo Control";
    private int m_counter;
    private bool m_isHighlighted;
    private double m_progress = 50;
    private string m_statusText = "Ready";
    private string m_statusMessage = "Application started";

    #endregion

    #region Constructors

    public MainViewModel()
    {
        InitCommands();
    }

    #endregion

    #region Initialization

    private void InitCommands()
    {
        // Initialize commands using cross-platform RelayCommand
        IncrementCommand = new RelayCommand(_ => Counter++);
        DecrementCommand = new RelayCommand(_ => Counter--, _ => Counter > 0);
        ResetCommand = new RelayCommand(_ => Counter = 0, _ => Counter != 0);
    }

    #endregion

    #region Functions

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    #endregion

    #region Properties

    public string ControlTitle
    {
        get => m_controlTitle;
        set => SetProperty(ref m_controlTitle, value);
    }

    public int Counter
    {
        get => m_counter;
        set
        {
            if (SetProperty(ref m_counter, value))
            {
                DecrementCommand.RaiseCanExecuteChanged();
                ResetCommand.RaiseCanExecuteChanged();
                StatusMessage = $"Counter changed to {value}";
            }
        }
    }

    public bool IsHighlighted
    {
        get => m_isHighlighted;
        set => SetProperty(ref m_isHighlighted, value);
    }

    public double Progress
    {
        get => m_progress;
        set
        {
            if (SetProperty(ref m_progress, value))
            {
                StatusMessage = $"Progress: {value:F0}%";
            }
        }
    }

    public string StatusText
    {
        get => m_statusText;
        set => SetProperty(ref m_statusText, value);
    }

    public string StatusMessage
    {
        get => m_statusMessage;
        set => SetProperty(ref m_statusMessage, value);
    }

    public RelayCommand IncrementCommand { get; private set; } = null!;
    public RelayCommand DecrementCommand { get; private set; } = null!;
    public RelayCommand ResetCommand { get; private set; } = null!;

    #endregion
}
