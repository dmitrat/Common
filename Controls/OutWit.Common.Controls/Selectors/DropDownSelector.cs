using System;
using System.Collections;
using System.Windows;
using OutWit.Common.Controls.DropDown;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Selectors
{
    public class DropDownSelector : DropDownBase
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty ItemsSourceProperty = BindingUtils.Register<DropDownSelector, IEnumerable>(nameof(ItemsSource));
        public static readonly DependencyProperty SelectedItemProperty = BindingUtils.Register<DropDownSelector, object>(nameof(SelectedItem));
        public static readonly DependencyProperty ItemTemplateProperty = BindingUtils.Register<DropDownSelector, DataTemplate>(nameof(ItemTemplate));

        #endregion

        #region Constructors

        static DropDownSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownSelector),
                new FrameworkPropertyMetadata(typeof(DropDownSelector)));
        }

        #endregion

        #region Bindable Properties
        
        [Bindable]
        public IEnumerable ItemsSource { get; set; }
        [Bindable]
        public object SelectedItem { get; set; }
        [Bindable]
        public DataTemplate ItemTemplate { get; set; }

        #endregion
    }
}
