using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    public class NumericInput : TextBox
    {
        #region DependencyProperties

        public static readonly DependencyProperty AllowFloatProperty = BindingUtils.Register<NumericInput, bool>(nameof(AllowFloat), true);
        public static readonly DependencyProperty AllowNegativeProperty = BindingUtils.Register<NumericInput, bool>(nameof(AllowNegative));

        #endregion

        #region Properties

        #region Constructors

        public NumericInput()
        {
            InitEvents();
        }

        #endregion


        #region Initialization

        private void InitEvents()
        {
            PreviewTextInput += OnPreviewTextInput;
        }


        #endregion

        #region Functions


        private void OnPreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var nDots = 0;
            foreach (var symbol in e.Text)
            {
                if (char.IsDigit(symbol))
                    continue;

                if (symbol == '.')
                    nDots++;
                else
                {
                    e.Handled = true;
                    break;
                }
            }

            if (nDots > 0 && !AllowFloat)
                e.Handled = true;

            if (nDots > 1)
                e.Handled = true;

            if (nDots == 1 && Text.Contains('.'))
                e.Handled = true;

        }

        #endregion

        [Bindable]
        public bool AllowFloat { get; set; }

        [Bindable]
        public bool AllowNegative { get; set; }
        #endregion
    }
}
