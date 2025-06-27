using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OutWit.Common.Locker;
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Settings.Values;
using OutWit.Common.Utils;

namespace OutWit.Common.Settings.Controls
{
    /// <summary>
    /// Interaction logic for SettingsValueEnumControl.xaml
    /// </summary>
    public partial class SettingsValueEnumControl : UserControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty ValueProperty = BindingUtils.Register<SettingsValueEnumControl, Enum>(nameof(Value), OnValueChanged);

        public static readonly DependencyProperty EnumTypeProperty = BindingUtils.Register<SettingsValueEnumControl, Type>(nameof(EnumType));

        #endregion

        #region Constructors

        public SettingsValueEnumControl()
        {
            InitializeComponent();
            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
        }

        #endregion

        #region Functions

        private void Reset()
        {
            using (new GlobalLocker())
            {
                SettingsValue = DataContext as SettingsValueEnum;
                if (SettingsValue == null)
                    return;

                EnumType = SettingsValue.UserValue.GetType();
                Value = SettingsValue.UserValue;

                SettingsValue.PropertyChanged += OnSettingPropertyChanged;
            }
            
        }

        private void UpdateSettingValue()
        {
            if(GlobalLocker.IsLocked())
                return;

            if (SettingsValue != null)
                SettingsValue.UserValue = Value;
        }

        #endregion

        #region EventHandlers

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if(e.IsProperty((SettingsValueEnumControl c)=>c.DataContext))
                Reset();

            base.OnPropertyChanged(e);
        }

        private void OnSettingPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.IsProperty((SettingsValueEnum val) => val.UserValue) && SettingsValue != null)
                Value = SettingsValue.UserValue;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SettingsValueEnumControl) d;
            control.UpdateSettingValue();
        }

        #endregion

        #region Properties

        [Bindable]
        public Enum Value { get; set; }

        [Bindable]
        public Type EnumType { get; set; }
        
        public SettingsValueEnum SettingsValue { get; set; }

        #endregion
    }
}
