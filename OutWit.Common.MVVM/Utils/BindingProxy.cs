using System;
using System.Windows;

namespace OutWit.Common.MVVM.Utils
{
    public class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty = BindingUtils.Register<BindingProxy, object>(nameof(Data));

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
