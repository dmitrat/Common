using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Panels
{
    public class ExpanderPanel : ContentControl
    {
        #region DependencyProperties
        
        public static readonly DependencyProperty IsExpandedProperty = BindingUtils.Register<ExpanderPanel, bool>(nameof(IsExpanded));

        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<ExpanderPanel, string>(nameof(Header));

        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<ExpanderPanel, string>(nameof(HeaderKey), OnHeaderKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<ExpanderPanel, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        
        #region Constructors

        static ExpanderPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExpanderPanel),
                new FrameworkPropertyMetadata(typeof(ExpanderPanel)));
        }

        public ExpanderPanel()
        {
            
        }

        #endregion

        #region Functions

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty, this.CreateBinding(x => x.HeaderKey, TextConverter));
        } 

        #endregion

        #region Events Handlers

        private static void OnHeaderKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ExpanderPanel)source;
            input.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (ExpanderPanel)source;
            input.ResetHeader();
        }


        #endregion

        #region Properties

        [Bindable]
        public bool IsExpanded { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        #endregion
    }
}
