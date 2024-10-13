using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OutWit.Common.Aspects.Utils;
using OutWit.Common.Collections;
using OutWit.Common.Controls.Converters;
using OutWit.Common.Locker;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Settings.Values;

namespace OutWit.Common.Settings.Controls
{
    /// <summary>
    /// Interaction logic for SettingsValueStringListControl.xaml
    /// </summary>
    public partial class SettingsValueStringListControl : UserControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty ItemsSourceProperty = BindingUtils.Register<SettingsValueStringListControl, ObservableCollectionEx<StringHolder>>(nameof(ItemsSource));

        public static readonly DependencyProperty SelectedIndexProperty = BindingUtils.Register<SettingsValueStringListControl, int>(nameof(SelectedIndex));

        public static readonly DependencyProperty ItemCommitEditCmdProperty = BindingUtils.Register<SettingsValueStringListControl, ICommand>(nameof(ItemCommitEditCmd));

        #endregion

        #region Constructors

        public SettingsValueStringListControl()
        {
            InitializeComponent();
            InitEvents();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {

        }

        private void InitCommands()
        {
            ItemCommitEditCmd = new DelegateCommand(x => ItemCommitEdit(x as StringHolder));
        }

        #endregion

        #region Functions

        private void ItemCommitEdit(StringHolder item)
        {
            if (item == null)
                return;

            if (string.IsNullOrWhiteSpace(item.Value) || ItemsSource.Count(x => x.AreSameAs(item)) > 1)
            {
                //var index = ItemsSource.IndexOf(item);

                ItemsSource.Remove(item);

                //SelectedIndex = Math.Min(ItemsSource.Count - 1, Math.Max(0, index - 1));

            }

        }

        private void Reset()
        {
            using (new GlobalLocker())
            {
                SettingsValue = DataContext as SettingsValueStringList;
                if (SettingsValue == null)
                    return;

                ResetItemsSource();

                SettingsValue.PropertyChanged += OnSettingPropertyChanged;
            }

        }

        private void ResetItemsSource()
        {
            UnsubscribeItemsSourceCollectionEvents();

            ItemsSource = new ObservableCollectionEx<StringHolder>(SettingsValue.UserValue.Select(x => new StringHolder(x)));
            
            SubscribeItemsSourceCollectionEvents();
        }

        private void UpdateSettingValue()
        {
            if(SettingsValue == null)
                return;

            using (new GlobalLocker())
            {
                SettingsValue.UserValue = ItemsSource.Select(holder => holder.Value).ToArray();
            }
        }

        private void UnsubscribeItemsSourceCollectionEvents()
        {
            if(ItemsSource == null)
                return;

            ItemsSource.CollectionChanged -= OnItemsSourceCollectionChanged;
            ItemsSource.CollectionContentChanged -= OnItemsSourceCollectionContentChanged;
        }

        private void SubscribeItemsSourceCollectionEvents()
        {
            if (ItemsSource == null)
                return;

            ItemsSource.CollectionChanged += OnItemsSourceCollectionChanged;
            ItemsSource.CollectionContentChanged += OnItemsSourceCollectionContentChanged;
        }

        #endregion

        #region Event Handlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.IsProperty((SettingsValueStringListControl c) => c.DataContext))
                Reset();

            base.OnPropertyChanged(e);
        }

        private void OnSettingPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(GlobalLocker.IsLocked())
                return;

            if (e.IsProperty((SettingsValueStringList val) => val.UserValue) && SettingsValue != null)
                ResetItemsSource();
        }

        private void OnItemsSourceCollectionContentChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateSettingValue();
        }

        private void OnItemsSourceCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateSettingValue();
        }

        #endregion

        #region Properties

        public SettingsValueStringList SettingsValue { get; set; }

        [Bindable]
        public ObservableCollectionEx<StringHolder> ItemsSource { get; set; }

        [Bindable]
        public int SelectedIndex { get; set; }

        [Bindable]
        public ICommand ItemCommitEditCmd { get; private set; }

        #endregion
    }
}
