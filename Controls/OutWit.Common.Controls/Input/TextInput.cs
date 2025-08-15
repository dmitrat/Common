using System;
using System.Windows;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    public class TextInput : InputBase<string>
    {
        #region DependencyProperties

        public static readonly DependencyProperty AcceptsReturnProperty = BindingUtils.Register<TextInput, bool>(nameof(AcceptsReturn));

        #endregion

        #region Constructors

        static TextInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextInput),
                new FrameworkPropertyMetadata(typeof(TextInput)));
        }

        #endregion

        #region Properties

        [Bindable]
        public bool AcceptsReturn { get; set; }

        #endregion
    }
}
