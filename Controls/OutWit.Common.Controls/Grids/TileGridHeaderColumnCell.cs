using System;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Grids
{
    public class TileGridHeaderColumnCell : Control
    {
		#region DependencyProperties

        public static readonly DependencyProperty TextProperty = BindingUtils.Register<TileGridHeaderColumnCell, string>(nameof(Text));
        public static readonly DependencyProperty TextKeyProperty = BindingUtils.Register<TileGridHeaderColumnCell, string>(nameof(TextKey), OnTextKeyChanged);
        public static readonly DependencyProperty TextStyleProperty = BindingUtils.Register<TileGridHeaderColumnCell, Style>(nameof(TextStyle));

        public static readonly DependencyProperty TextWrappingProperty = BindingUtils.Register<TileGridHeaderColumnCell, TextWrapping>(nameof(TextWrapping));

        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<TileGridHeaderColumnCell, string>(nameof(Header));
        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<TileGridHeaderColumnCell, string>(nameof(HeaderKey), OnHeaderKeyChanged);
        public static readonly DependencyProperty HeaderStyleProperty = BindingUtils.Register<TileGridHeaderColumnCell, Style>(nameof(HeaderStyle));

		public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<TileGridHeaderColumnCell, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);
		
		#endregion

		#region Constructors
		static TileGridHeaderColumnCell()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TileGridHeaderColumnCell),
				new FrameworkPropertyMetadata(typeof(TileGridHeaderColumnCell)));
		}

		public TileGridHeaderColumnCell()
		{
		}

		#endregion

		#region Functions

		private void ResetText()
		{
			if (TextConverter == null || string.IsNullOrEmpty(TextKey))
				return;

			SetBinding(TextProperty, this.CreateBinding(x => x.TextKey, TextConverter));
		}

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
			var cell = (TileGridHeaderColumnCell)source;
			cell.ResetHeader();
		}

		private static void OnTextKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var cell = (TileGridHeaderColumnCell)source;
			cell.ResetText();
		}

		private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var cell = (TileGridHeaderColumnCell)source;
			cell.ResetText();
			cell.ResetHeader();
		}

		#endregion

		#region Properties

		[Bindable]
		public string Text { get; set; }

        [Bindable]
        public string TextKey { get; set; }

        [Bindable]
        public Style TextStyle { get; set; }


        [Bindable]
        public TextWrapping TextWrapping { get; set; }

        [Bindable]
        public string Header { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }


        [Bindable]
        public Style HeaderStyle { get; set; }

        [Bindable] 
        public StringToResourceConverterBase TextConverter { get; set; }

        #endregion
	}
}
