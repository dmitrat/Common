using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AspectInjector.Broker;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Prompts
{
    public class InfoPrompt : Control
    {
        #region DependencyProperties

        public static readonly DependencyProperty KindProperty = BindingUtils.Register<InfoPrompt, PackIconKind?>(nameof(Kind));

        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<InfoPrompt, string>(nameof(Header));
        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<InfoPrompt, string>(nameof(HeaderKey), OnHeaderKeyChanged);
        public static readonly DependencyProperty HeaderBackgroundProperty = BindingUtils.Register<InfoPrompt, Brush>(nameof(HeaderBackground), Brushes.White);
        public static readonly DependencyProperty HeaderForegroundProperty = BindingUtils.Register<InfoPrompt, Brush>(nameof(HeaderForeground), Brushes.Black);

        public static readonly DependencyProperty ButtonsForegroundProperty = BindingUtils.Register<InfoPrompt, Brush>(nameof(ButtonsForeground), Brushes.Black);

        public static readonly DependencyProperty TextSourceProperty = BindingUtils.Register<InfoPrompt, IEnumerable<string>>(nameof(TextSource));
        public static readonly DependencyProperty OptionSourceProperty = BindingUtils.Register<InfoPrompt, Array>(nameof(OptionSource));
        public static readonly DependencyProperty OptionCancelProperty = BindingUtils.Register<InfoPrompt, Enum>(nameof(OptionCancel));
        public static readonly DependencyProperty OptionDefaultProperty = BindingUtils.Register<InfoPrompt, Enum>(nameof(OptionDefault));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<InfoPrompt, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        #endregion

        #region Constructors

        static InfoPrompt()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoPrompt),
                new FrameworkPropertyMetadata(typeof(InfoPrompt)));
        }

        public InfoPrompt()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            this.KeyDown += OnKeyDown;
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
            var input = (InfoPrompt)source;
            input.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (InfoPrompt)source;
            input.ResetHeader();
        }


        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && OptionDefault != null)
                DialogHost.CloseDialogCommand.Execute(OptionDefault, this);

            if (e.Key == Key.Escape && OptionCancel != null)
                DialogHost.CloseDialogCommand.Execute(OptionCancel, this);

        }

        #endregion

        #region Properties

        [Bindable]
        public PackIconKind? Kind { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public Brush HeaderBackground { get; set; }

        [Bindable]
        public Brush HeaderForeground { get; set; }

        [Bindable]
        public Brush ButtonsForeground { get; set; }

        [Bindable]
        public IEnumerable<string> TextSource { get; set; }

        [Bindable]
        public Array OptionSource { get; set; }

        [Bindable]
        public Enum OptionCancel { get; set; }

        [Bindable]
        public Enum OptionDefault { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        #endregion
    }
}
