using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.MVVM.WPF.Commands;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Samples.Serializers.Types;

namespace OutWit.Common.Settings.Samples.Wpf.ViewModels
{
    /// <summary>
    /// Wrapper around <see cref="ISettingsValue"/> for WPF data binding.
    /// </summary>
    public sealed class SettingsValueViewModel : ViewModelBase<ApplicationViewModel>, INotifyPropertyChanged
    {
        #region Fields

        private readonly ISettingsValue m_settingsValue;

        #endregion

        #region Constructors

        public SettingsValueViewModel(ApplicationViewModel appVm, ISettingsValue settingsValue)
            : base(appVm)
        {
            m_settingsValue = settingsValue;
            m_settingsValue.PropertyChanged += OnSettingsValuePropertyChanged;

            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            ResetCommand = new DelegateCommand(_ => Reset(), _ => !IsDefault);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Resets the user value to the default value.
        /// </summary>
        public void Reset()
        {
            Value = m_settingsValue.DefaultValue;
        }

        /// <summary>
        /// Gets available enum values when ValueKind is "Enum".
        /// </summary>
        public IReadOnlyList<string> GetEnumValues()
        {
            if (string.IsNullOrEmpty(Tag))
                return Array.Empty<string>();

            var type = Type.GetType(Tag);
            if (type == null || !type.IsEnum)
                return Array.Empty<string>();

            return Enum.GetNames(type).ToList();
        }

        #endregion

        #region Event Handlers

        private void OnSettingsValuePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettingsValue.Value))
            {
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(IsDefault));
                OnPropertyChanged(nameof(SelectedEnumName));
                OnPropertyChanged(nameof(BoundedValue));
                OnPropertyChanged(nameof(BoundedMin));
                OnPropertyChanged(nameof(BoundedMax));
                OnPropertyChanged(nameof(ColorR));
                OnPropertyChanged(nameof(ColorG));
                OnPropertyChanged(nameof(ColorB));
                OnPropertyChanged(nameof(ColorPreviewBrush));
                CommandManager.InvalidateRequerySuggested();
            }
            else if (e.PropertyName == nameof(ISettingsValue.IsDefault))
            {
                OnPropertyChanged(nameof(IsDefault));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the display name (defaults to Key).
        /// </summary>
        public string Name => m_settingsValue.Name;

        /// <summary>
        /// Gets the unique key within the group.
        /// </summary>
        public string Key => m_settingsValue.Key;

        /// <summary>
        /// Gets the serializer kind (determines which editor to show).
        /// </summary>
        public string ValueKind => m_settingsValue.ValueKind;

        /// <summary>
        /// Gets additional metadata (e.g. enum type name).
        /// </summary>
        public string Tag => m_settingsValue.Tag;

        /// <summary>
        /// Gets or sets the current user value.
        /// </summary>
        public object Value
        {
            get => m_settingsValue.Value;
            set
            {
                m_settingsValue.Value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        /// <summary>
        /// Gets whether the current value equals the default.
        /// </summary>
        public bool IsDefault => m_settingsValue.IsDefault;

        /// <summary>
        /// Gets the default value.
        /// </summary>
        public object DefaultValue => m_settingsValue.DefaultValue;

        #endregion

        #region Enum Properties

        /// <summary>
        /// Gets the list of enum value names for ComboBox binding.
        /// </summary>
        public IReadOnlyList<string> EnumValues => GetEnumValues();

        /// <summary>
        /// Gets or sets the selected enum value as a string name.
        /// </summary>
        public string? SelectedEnumName
        {
            get => Value?.ToString();
            set
            {
                if (value == null || string.IsNullOrEmpty(Tag))
                    return;

                var type = Type.GetType(Tag);
                if (type != null && type.IsEnum && Enum.TryParse(type, value, out var parsed))
                    Value = parsed;
            }
        }

        #endregion

        #region BoundedInt Properties

        /// <summary>
        /// Gets or sets the BoundedInt.Value component.
        /// </summary>
        public int BoundedValue
        {
            get => Value is BoundedInt bi ? bi.Value : 0;
            set
            {
                if (Value is BoundedInt bi)
                {
                    Value = new BoundedInt { Value = value, Min = bi.Min, Max = bi.Max };
                    OnPropertyChanged(nameof(BoundedValue));
                }
            }
        }

        /// <summary>
        /// Gets the BoundedInt.Min component.
        /// </summary>
        public int BoundedMin => Value is BoundedInt bi ? bi.Min : 0;

        /// <summary>
        /// Gets the BoundedInt.Max component.
        /// </summary>
        public int BoundedMax => Value is BoundedInt bi ? bi.Max : 100;

        #endregion

        #region ColorRgb Properties

        /// <summary>
        /// Gets or sets the red component.
        /// </summary>
        public double ColorR
        {
            get => Value is ColorRgb c ? c.R : 0;
            set
            {
                if (Value is ColorRgb c)
                {
                    Value = new ColorRgb { R = (byte)value, G = c.G, B = c.B };
                    OnPropertyChanged(nameof(ColorR));
                    OnPropertyChanged(nameof(ColorPreviewBrush));
                }
            }
        }

        /// <summary>
        /// Gets or sets the green component.
        /// </summary>
        public double ColorG
        {
            get => Value is ColorRgb c ? c.G : 0;
            set
            {
                if (Value is ColorRgb c)
                {
                    Value = new ColorRgb { R = c.R, G = (byte)value, B = c.B };
                    OnPropertyChanged(nameof(ColorG));
                    OnPropertyChanged(nameof(ColorPreviewBrush));
                }
            }
        }

        /// <summary>
        /// Gets or sets the blue component.
        /// </summary>
        public double ColorB
        {
            get => Value is ColorRgb c ? c.B : 0;
            set
            {
                if (Value is ColorRgb c)
                {
                    Value = new ColorRgb { R = c.R, G = c.G, B = (byte)value };
                    OnPropertyChanged(nameof(ColorB));
                    OnPropertyChanged(nameof(ColorPreviewBrush));
                }
            }
        }

        /// <summary>
        /// Gets a SolidColorBrush preview of the current color.
        /// </summary>
        public Brush ColorPreviewBrush
        {
            get
            {
                if (Value is ColorRgb c)
                    return new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));

                return Brushes.Transparent;
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Resets this setting to its default value.
        /// </summary>
        public DelegateCommand ResetCommand { get; private set; } = null!;

        #endregion
    }
}
