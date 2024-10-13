using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using OutWit.Common.Controls.Converters;
using OutWit.Common.MVVM.Utils;

namespace OutWit.Common.Controls.Grids
{
    public class TileGridDateTimeColumn : TileGridHeaderColumn
    {

		#region Constructors

		static TileGridDateTimeColumn()
		{
		}

		public TileGridDateTimeColumn()
		{
		}

		#endregion
		
		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			var cellControl = new TileGridDateTimeColumnCell();

			cell.Background = Brushes.Transparent;

			if (Binding != null)
				cellControl.SetBinding(TileGridDateTimeColumnCell.ValueProperty, Binding);

			if (TextStyle != null)
				cellControl.SetBinding(TileGridDateTimeColumnCell.TextStyleProperty, this.CreateBinding(x => x.TextStyle));

			if (Header != null)
				cellControl.SetBinding(TileGridDateTimeColumnCell.HeaderProperty, this.CreateBinding(x => x.Header));

			if (!string.IsNullOrEmpty(HeaderKey))
				cellControl.SetBinding(TileGridDateTimeColumnCell.HeaderKeyProperty, this.CreateBinding(x => x.HeaderKey));

			if (HeaderStyle != null)
				cellControl.SetBinding(TileGridDateTimeColumnCell.HeaderStyleProperty, this.CreateBinding(x => x.HeaderStyle));

			if (TextConverter != null)
				cellControl.SetBinding(TileGridDateTimeColumnCell.TextConverterProperty, this.CreateBinding(x => x.TextConverter));

			cellControl.SetBinding(TileGridDateTimeColumnCell.TextWrappingProperty, this.CreateBinding(x => x.TextWrapping));
			cellControl.SetBinding(TileGridDateTimeColumnCell.MarginProperty, this.CreateBinding(x => x.Margin));

            return cellControl;
		}
	}
}
