using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using OutWit.Common.Collections;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Input;
using OutWit.Common.Locker;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Utils;

namespace OutWit.Common.Controls.Selectors
{
    public class DropDownComboSelector : ItemsControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty ImageKindProperty = BindingUtils.Register<DropDownComboSelector, PackIconKind>(nameof(ImageKind));
        public static readonly DependencyProperty ImageSizeProperty = BindingUtils.Register<DropDownComboSelector, double>(nameof(ImageSize));
        public static readonly DependencyProperty ShowImageProperty = BindingUtils.Register<DropDownComboSelector, bool>(nameof(ShowImage));

        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<DropDownComboSelector, string>(nameof(HeaderKey), OnHeaderTextChanged);
        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<DropDownComboSelector, string>(nameof(Header));
        public static readonly DependencyProperty HeaderScaleProperty = BindingUtils.Register<DropDownComboSelector, double>(nameof(HeaderScale), OnHeaderScaleChanged);
        public static readonly DependencyProperty HintFontSizeProperty = BindingUtils.Register<DropDownComboSelector, double>(nameof(HintFontSize));

        public static readonly DependencyProperty TextWrappingProperty = BindingUtils.Register<DropDownComboSelector, TextWrapping>(nameof(TextWrapping), TextWrapping.NoWrap);

        public static readonly DependencyProperty IsReadOnlyProperty = BindingUtils.Register<DropDownComboSelector, bool>(nameof(IsReadOnly));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<DropDownComboSelector, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);
        
        public static readonly DependencyProperty SelectedItemProperty = BindingUtils.Register<DropDownComboSelector, object>(nameof(SelectedItem), OnSelectionChanged);

        public static readonly DependencyProperty IsPopupOpenProperty = BindingUtils.Register<DropDownComboSelector, bool>(nameof(IsPopupOpen));

        public static readonly DependencyProperty CheckPopupCmdProperty = BindingUtils.Register<DropDownComboSelector, ICommand>(nameof(CheckPopupCmd));

        #endregion

        #region Constructors

        static DropDownComboSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownComboSelector),
                new FrameworkPropertyMetadata(typeof(DropDownComboSelector)));
        }

        public DropDownComboSelector()
        {
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            //this.Loaded += OnLoaded;
            
        }

        private void InitCommands()
        {
            CheckPopupCmd = new DelegateCommand(x => CheckPopup());
        }

        #endregion

        #region Functions

        private void CheckPopup()
        {
            IsPopupOpen = !IsPopupOpen;

            Trace.WriteLine($"DropDownComboSelector mouse up, IsPopupOpen: {IsPopupOpen}");
        }

        private void ClosePopup()
        {
            if (IsPopupOpen)
                IsPopupOpen = false;
        }

        private void ResetHeader()
        {
            if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
                return;

            SetBinding(HeaderProperty, new Binding { Converter = TextConverter, ConverterParameter = HeaderKey });
        }

        private void UpdateHintFontSize()
        {
            HintFontSize = FontSize * HeaderScale;
        }

        #endregion

        #region Event Handlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if(e.IsProperty((DropDownComboSelector s)=>s.FontSize))
                UpdateHintFontSize();

            base.OnPropertyChanged(e);
        }

        private static void OnHeaderScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownComboSelector)d;
            selector.UpdateHintFontSize();
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownComboSelector)d;
            //selector.ClosePopup();
        }

        private static void OnHeaderTextChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownComboSelector)source;
            selector.ResetHeader();
        }

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DropDownComboSelector)source;
            selector.ResetHeader();
        }

        #endregion

        #region Properties

        [Bindable]
        public PackIconKind ImageKind { get; set; }

        [Bindable]
        public double ImageSize { get; set; }

        [Bindable]
        public bool ShowImage { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public double HeaderScale { get; set; }

        [Bindable]
        public double HintFontSize { get; set; }

        [Bindable]
        public TextWrapping TextWrapping { get; set; }

        [Bindable]
        public bool IsReadOnly { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }
        
        [Bindable]
        public object SelectedItem { get; set; }

        [Bindable]
        public bool IsPopupOpen { get; set; }

        [Bindable]
        public ICommand CheckPopupCmd { get; set; }

        #endregion
    }
}
