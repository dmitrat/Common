using System;
using System.Windows;
using System.Windows.Controls;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Grids
{
    public class TileGridDateTimeColumnCell : Control
    {
		#region DependencyProperties

        public static readonly DependencyProperty ValueProperty = BindingUtils.Register<TileGridDateTimeColumnCell, DateTime?>(nameof(Value));
        public static readonly DependencyProperty TextStyleProperty = BindingUtils.Register<TileGridDateTimeColumnCell, Style>(nameof(TextStyle));

        public static readonly DependencyProperty TextWrappingProperty = BindingUtils.Register<TileGridDateTimeColumnCell, TextWrapping>(nameof(TextWrapping));

        public static readonly DependencyProperty HeaderProperty = BindingUtils.Register<TileGridDateTimeColumnCell, string>(nameof(Header));
        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<TileGridDateTimeColumnCell, string>(nameof(HeaderKey), OnHeaderKeyChanged);
        public static readonly DependencyProperty HeaderStyleProperty = BindingUtils.Register<TileGridDateTimeColumnCell, Style>(nameof(HeaderStyle));

		public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<TileGridDateTimeColumnCell, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);
		
		#endregion

		#region Constructors
		static TileGridDateTimeColumnCell()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TileGridDateTimeColumnCell),
				new FrameworkPropertyMetadata(typeof(TileGridDateTimeColumnCell)));
		}

		public TileGridDateTimeColumnCell()
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
			var cell = (TileGridDateTimeColumnCell)source;
			cell.ResetHeader();
		}

		private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var cell = (TileGridDateTimeColumnCell)source;
			cell.ResetHeader();
		}

		#endregion

		#region Properties

		[Bindable]
		public DateTime? Value { get; set; }

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
