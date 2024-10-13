using System;
using System.Windows;
using System.Windows.Media;

namespace OutWit.Common.Controls.HighlightTextBox
{
    public class DrawingControl : FrameworkElement 
    {
        #region Constructors

        public DrawingControl()
        {
            InitDefaults();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Visual = new DrawingVisual();
            VisualChildren = new VisualCollection(this) { Visual };
        }

        #endregion

        #region Functions

        public DrawingContext GetContext()
        {
            return Visual.RenderOpen();
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= VisualChildren.Count)
                throw new ArgumentOutOfRangeException();

            return VisualChildren[index];
        } 

        #endregion

        #region Properties

        protected override int VisualChildrenCount => VisualChildren.Count;

        public VisualCollection VisualChildren { get; private set; }

        public DrawingVisual Visual { get; private set; } 

        #endregion
    }
}
