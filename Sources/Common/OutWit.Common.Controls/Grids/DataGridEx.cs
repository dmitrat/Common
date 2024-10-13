using OutWit.Common.Controls.Buttons;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Controls.Output;
using OutWit.Common.MVVM.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows;
using OutWit.Common.MVVM.Aspects;

namespace OutWit.Common.Controls.Grids
{
    public class DataGridEx : DataGrid
    {
        #region DependencyProperties


        public static readonly DependencyProperty NewRowTextProperty = BindingUtils.Register<DataGridEx, string>(nameof(NewRowText));
        public static readonly DependencyProperty NewRowTextKeyProperty = BindingUtils.Register<DataGridEx, string>(nameof(NewRowTextKey), OnNewRowTextKeyChanged);

        public static readonly DependencyProperty NewRowStyleProperty = BindingUtils.Register<DataGridEx, Style>(nameof(NewRowStyle));

        public static readonly DependencyProperty TextConverterProperty = BindingUtils.Register<DataGridEx, StringToResourceConverterBase>(nameof(TextConverter), OnTextConverterChanged);

        public static readonly DependencyProperty ItemCommitEditCmdProperty = BindingUtils.Register<DataGridEx, ICommand>(nameof(ItemCommitEditCmd));

        #endregion

        #region Constructors

        public DataGridEx()
        {
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            ItemContainerGenerator.StatusChanged += ItemContainerGeneratorStatusChanged;
        }

        #endregion

        #region Functions

        private void ResetNewRowText()
        {
            if (TextConverter == null || string.IsNullOrEmpty(NewRowTextKey))
                return;

            SetBinding(NewRowTextProperty, new Binding { Converter = TextConverter, ConverterParameter = NewRowTextKey });
        }

        protected override void OnExecutedCommitEdit(ExecutedRoutedEventArgs e)
        {
            base.OnExecutedCommitEdit(e);
            ItemCommitEditCmd?.Execute((e.OriginalSource as FrameworkElement)?.DataContext);
        }

        #endregion

        #region Event Handlers

        private static void OnTextConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (DataGridEx)source;
            input.ResetNewRowText();
        }

        private static void OnNewRowTextKeyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var input = (DataGridEx)source;
            input.ResetNewRowText();
        }

        private void ItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            var dataGridRow = ItemContainerGenerator.ContainerFromItem(CollectionView.NewItemPlaceholder) as DataGridRow;

            var textBlock = dataGridRow?.FindFirstChildOf<TextBlock>();
            if (textBlock == null)
                return;

            foreach (var button in dataGridRow.FindAllChildrenOf<FlatButton>())
                button.Visibility = Visibility.Collapsed;

            foreach (var text in dataGridRow.FindAllChildrenOf<TextOutputEx>())
                text.Visibility = Visibility.Collapsed;


            textBlock.SetBinding(TextBlock.TextProperty, this.CreateBinding(x => x.NewRowText));
            textBlock.SetBinding(StyleProperty, this.CreateBinding(x => x.NewRowStyle));

        }

        #endregion

        #region Properties

        [Bindable]
        public string NewRowText { get; set; }

        [Bindable]
        public string NewRowTextKey { get; set; }

        [Bindable]
        public Style NewRowStyle { get; set; }

        [Bindable]
        public StringToResourceConverterBase TextConverter { get; set; }

        [Bindable]
        public ICommand ItemCommitEditCmd { get; set; }

        #endregion
    }

    public delegate void DataGridExItemEventHandler(object item);
}
