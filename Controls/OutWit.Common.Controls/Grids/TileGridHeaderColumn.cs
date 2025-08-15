using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Grids
{
    public class TileGridHeaderColumn : DataGridBoundColumn
    {
		#region DependencyProperties
		
        public static readonly DependencyProperty MarginProperty = BindingUtils.Register<TileGridHeaderColumn, Thickness>(nameof(Margin), new Thickness(0));
        public static readonly DependencyProperty TextStyleProperty = BindingUtils.Register<TileGridHeaderColumn, Style>(nameof(TextStyle));
        public static readonly DependencyProperty TextWrappingProperty = BindingUtils.Register<TileGridHeaderColumn, TextWrapping>(nameof(TextWrapping), TextWrapping.NoWrap);

        public static readonly DependencyProperty HeaderKeyProperty = BindingUtils.Register<TileGridHeaderColumn, string>(nameof(HeaderKey), OnHeaderKeyChanged);

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<TileGridHeaderColumn, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);
		
		#endregion

		#region Constructors

		static TileGridHeaderColumn()
		{
		}

		public TileGridHeaderColumn()
		{
		}

		#endregion

		#region Functions

		private void ResetHeader()
		{
			if (TextConverter == null || string.IsNullOrEmpty(HeaderKey))
				return;
			
			BindingOperations.SetBinding(this, HeaderProperty, this.CreateBinding(x => x.HeaderKey, TextConverter));
		}

		#endregion

		#region Events Handlers

		private static void OnHeaderKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var column = (TileGridHeaderColumn)source;
			column.ResetHeader();
		}

		private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
		{
			var column = (TileGridHeaderColumn)source;
			column.ResetHeader();
		}

		#endregion

		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			var cellControl = new TileGridHeaderColumnCell();

			cell.Background = Brushes.Transparent;

			if (Binding != null)
				cellControl.SetBinding(TileGridHeaderColumnCell.TextProperty, Binding);

			if (TextStyle != null)
				cellControl.SetBinding(TileGridHeaderColumnCell.TextStyleProperty, this.CreateBinding(x => x.TextStyle));

			if (Header != null)
				cellControl.SetBinding(TileGridHeaderColumnCell.HeaderProperty, this.CreateBinding(x => x.Header));

			if (!string.IsNullOrEmpty(HeaderKey))
				cellControl.SetBinding(TileGridHeaderColumnCell.HeaderKeyProperty, this.CreateBinding(x => x.HeaderKey));

			if (HeaderStyle != null)
				cellControl.SetBinding(TileGridHeaderColumnCell.HeaderStyleProperty, this.CreateBinding(x => x.HeaderStyle));

			if (TextConverter != null)
				cellControl.SetBinding(TileGridHeaderColumnCell.TextConverterProperty, this.CreateBinding(x => x.TextConverter));

			cellControl.SetBinding(TileGridHeaderColumnCell.TextWrappingProperty, this.CreateBinding(x => x.TextWrapping));
			cellControl.SetBinding(TileGridHeaderColumnCell.MarginProperty, this.CreateBinding(x => x.Margin));

            return cellControl;
		}

		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{
			return GenerateElement(cell, dataItem);
		}

		#region Properties

		[Bindable]
		public Thickness Margin { get; set; }

        [Bindable]
        public Style TextStyle { get; set; }

        [Bindable]
        public TextWrapping TextWrapping { get; set; }

        [Bindable]
        public string HeaderKey { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

		#endregion
	}
}
