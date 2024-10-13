using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OutWit.Common.Controls.ToolBars
{
    public class ToolBarTrayEx : ToolBarTray
    {
        #region Constructors

        public ToolBarTrayEx()
        {
            InitEvents();
        } 

        #endregion

        #region Initialization

        private void InitEvents()
        {
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.SizeChanged += OnSizeChanged;
        } 

        #endregion

        #region Functions

        private bool HasOverflow()
        {
            return MainToolBars.Any(x => x.IsOverflow);
        }

        private double ToolBarsWidth()
        {
            return MainToolBars.Sum(x => x.ActualWidth);
        }

        #endregion

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
        }

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MainToolBars = ToolBars.OfType<ToolBarEx>().ToList();
            Separator = ToolBars.OfType<ToolBarSeparator>().SingleOrDefault();

            foreach (var toolbar in MainToolBars)
                toolbar.OverflowStatusChanged += OnOverflowStatusChanged;

            OnOverflowStatusChanged(null);
            
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var toolbar in MainToolBars)
                toolbar.OverflowStatusChanged -= OnOverflowStatusChanged;

            MainToolBars = null;
            Separator = null;
        }

        private void OnOverflowStatusChanged(ToolBarEx sender)
        {
            if(Separator == null || HasOverflow())
                return;

            Separator.Width = ActualWidth - ToolBarsWidth();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(Separator == null)
                return;

            if (Separator.Width > 0)
                Separator.Width = Math.Max(Separator.Width + e.NewSize.Width - e.PreviousSize.Width, 0);
        }

        #endregion

        #region Properties

        private ICollection<ToolBarEx> MainToolBars { get; set; }

        private ToolBarSeparator Separator { get; set; }

        #endregion
    }
}
