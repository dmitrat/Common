using System;
using System.Windows;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Input
{
    public class EnumSelectorInput : EnumComboInput
    {
        #region DependencyProperties

        public static readonly DependencyProperty HeaderFontSizeProperty = BindingUtils.Register<EnumSelectorInput, double>(nameof(HeaderFontSize));

        #endregion

        #region Constructors

        static EnumSelectorInput()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EnumSelectorInput),
                new FrameworkPropertyMetadata(typeof(EnumSelectorInput)));
        }

        public EnumSelectorInput()
        {
            InitDefaults();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            HeaderFontSize = FontSize * HeaderScale;
        }

        #endregion

        #region Events Handlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsProperty((EnumSelectorInput i) => i.FontSize))
                HeaderFontSize = FontSize * HeaderScale;

            if (e.IsProperty((EnumSelectorInput i) => i.HeaderScale))
                HeaderFontSize = FontSize * HeaderScale;
        }

        #endregion

        #region Properties

        [Bindable]
        internal double HeaderFontSize { get; set; }

        #endregion

    }
}
