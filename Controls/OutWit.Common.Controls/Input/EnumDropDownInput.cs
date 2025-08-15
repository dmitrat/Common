using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using OutWit.Common.Controls.DropDown;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    public class EnumDropDownInput : DropDownBase
    {
        #region DependencyProperties

        public static readonly DependencyProperty EnumTypeProperty = BindingUtils.Register<EnumDropDownInput, Type>(nameof(EnumType), OnEnumTypeChanged);
        public static readonly DependencyProperty ValueProperty = BindingUtils.Register<EnumDropDownInput, Enum>(nameof(Value));

        internal static readonly DependencyProperty EnumNamesProperty = BindingUtils.Register<EnumDropDownInput, IEnumerable<string>>(nameof(EnumNames));
        internal static readonly DependencyProperty SelectedEnumNameProperty = BindingUtils.Register<EnumDropDownInput, string>(nameof(SelectedEnumName), OnSelectedItemChanged);

        #endregion

        #region Constructors

        static EnumDropDownInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EnumDropDownInput),
                new FrameworkPropertyMetadata(typeof(EnumDropDownInput)));
        }

        #endregion

        #region Functions

        private void ResetValue()
        {
            if (string.IsNullOrEmpty(SelectedEnumName))
                return;

            Value = (Enum)Enum.Parse(EnumType, SelectedEnumName);
        }

        private void ResetItems()
        {
            EnumNames = Enum.GetNames(EnumType);
            SelectedEnumName = Value == null ? EnumNames.First() : Enum.GetName(EnumType, Value);
        }

        private void ResetSelected()
        {
            if (Value == null)
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

            if (e.IsProperty((EnumDropDownInput i) => i.Value))
                ResetSelected();
        }

        private static void OnEnumTypeChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (EnumDropDownInput)source;
            input.ResetItems();
        }

        private static void OnSelectedItemChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (EnumDropDownInput)source;
            input.ResetValue();
        }

        #endregion

        #region Properties

        [Bindable]
        public Type EnumType { get; set; }

        [Bindable]
        public Enum Value { get; set; }

        [Bindable]
        internal IEnumerable<string> EnumNames { get; set; }

        [Bindable]
        internal string SelectedEnumName { get; set; }

        #endregion
    }
}
