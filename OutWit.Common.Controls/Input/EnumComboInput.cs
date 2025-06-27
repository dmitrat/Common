using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    public class EnumComboInput : InputBase<Enum>
    {
        #region DependencyProperties

        public static readonly DependencyProperty EnumTypeProperty = BindingUtils.Register<EnumComboInput, Type>(nameof(EnumType), OnEnumTypeChanged);

        internal static readonly DependencyProperty EnumNamesProperty = BindingUtils.Register<EnumComboInput, IEnumerable<string>>(nameof(EnumNames));
        internal static readonly DependencyProperty SelectedEnumNameProperty = BindingUtils.Register<EnumComboInput, string>(nameof(SelectedEnumName), OnSelectedItemChanged);

        #endregion

        #region Constructors

        static EnumComboInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EnumComboInput),
                new FrameworkPropertyMetadata(typeof(EnumComboInput)));
        }

        #endregion

        #region Functions

        private void ResetValue()
        {
            if (string.IsNullOrEmpty(SelectedEnumName))
                SelectedEnumName = $"{Value}";
            else if(SelectedEnumName != $"{Value}")
                Value = (Enum)Enum.Parse(EnumType, SelectedEnumName);
        }

        private void ResetItems()
        {
            EnumNames = Enum.GetNames(EnumType);
            SelectedEnumName = Value == null ? EnumNames.First() : Enum.GetName(EnumType, Value);
        }

        private void ResetSelected()
        {
            if(Value == null)
                return;

            var item = Enum.GetName(EnumType, Value);
            if (SelectedEnumName != item)
                SelectedEnumName = item;
        }

        #endregion

        #region Event Handlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsProperty((EnumComboInput i) => i.Value))
                ResetSelected();
        }

        private static void OnEnumTypeChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (EnumComboInput)source;
            input.ResetItems();
        }

        private static void OnSelectedItemChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (EnumComboInput)source;
            input.ResetValue();
        }

        #endregion

        #region Properties

        [Bindable]
        internal IEnumerable<string> EnumNames { get; set; }

        [Bindable]
        internal string SelectedEnumName { get; set; }

        [Bindable]
        public Type EnumType { get; set; }

        #endregion
    }
}
