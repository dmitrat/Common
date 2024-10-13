using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    [DefaultProperty(nameof(Content))]
    [System.Windows.Markup.ContentProperty(nameof(Content))]
    public class CustomInput : InputBase
    {
        #region DependencyProperties

        public static readonly DependencyProperty ContentProperty = BindingUtils.Register<CustomInput, object>(nameof(Content));

        public static readonly DependencyProperty ShowHeaderProperty = BindingUtils.Register<CustomInput, bool>(nameof(ShowHeader));

        #endregion

        #region Constructors

        static CustomInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomInput),
                new FrameworkPropertyMetadata(typeof(CustomInput)));
        }

        #endregion


        #region Properties

        [MVVM.Aspects.Bindable]
        public object Content { get; set; }

        [MVVM.Aspects.Bindable]
        public bool ShowHeader { get; set; }

        #endregion

    }
}
