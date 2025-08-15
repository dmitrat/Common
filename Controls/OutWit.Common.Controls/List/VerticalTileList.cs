using System.Windows;
using System.Windows.Controls;

namespace OutWit.Common.Controls.List
{
    public class VerticalTileList : ListBox
    {
        #region Constructors

        static VerticalTileList()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VerticalTileList),
                new FrameworkPropertyMetadata(typeof(VerticalTileList)));
        }

        #endregion
    }
}
