using System.ComponentModel;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Attributes;
using OutWit.Common.Utils;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Model
{
    public class SelectableValue<TValue> : ModelBase
    {
        #region Events

        public event SelectableValueEventHandler<TValue> SelectionChanged = delegate { };

        #endregion

        #region Constructors

        public SelectableValue(TValue value, bool isSelected = true)
        {
            Value = value;
            IsSelected = isSelected;

            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            this.PropertyChanged += OnPropertyChanged;
        }

        #endregion

        #region Functions
        
        public void ToggleSelection()
        {
            IsSelected = !IsSelected;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if(modelBase is not SelectableValue<TValue> other)
                return false;

            return Value.Check(other.Value) 
                   && IsSelected.Is(other.IsSelected);
        }

#if NET6_0_OR_GREATER
        public override SelectableValue<TValue> Clone()
#else
        public override ModelBase Clone()
#endif
        {
            return new SelectableValue<TValue>(Value, IsSelected);
        }

        #endregion

        #region Operators

        public static explicit operator TValue(SelectableValue<TValue> selectableValue)
        {
            return selectableValue.Value;
        }

        public static implicit operator SelectableValue<TValue>(TValue value)
        {
            return new SelectableValue<TValue>(value);
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((SelectableValue<TValue> v) => v.IsSelected))
                SelectionChanged(this);
        }

        #endregion

        #region Properties

        [ToString]
        public TValue Value { get; }

        [ToString]
        [Notify]
        public bool IsSelected { get; set; }

        #endregion

    }

    public delegate void SelectableValueEventHandler<TValue>(SelectableValue<TValue> sender);
}
