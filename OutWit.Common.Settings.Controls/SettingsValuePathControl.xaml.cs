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
using OutWit.Common.MVVM.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Utils;
using OutWit.Common.Settings.Values;
using OutWit.Common.Utils;
using Ookii.Dialogs.Wpf;

namespace OutWit.Common.Settings.Controls
{
    /// <summary>
    /// Interaction logic for SettingsValuePathControl.xaml
    /// </summary>
    public partial class SettingsValuePathControl : UserControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty SelectFileCmdProperty = BindingUtils.Register<SettingsValuePathControl, ICommand>(nameof(SelectFileCmd));

        #endregion

        #region Constructors

        public SettingsValuePathControl()
        {
            InitializeComponent();
            InitCommands();
        } 

        #endregion

        #region Initialization

        private void InitCommands()
        {
            SelectFileCmd = new DelegateCommand(x => SelectFile(x as SettingsValuePath));
        }

        #endregion

        #region Functions

        private void SelectFile(SettingsValuePath setting)
        {
            if (setting == null)
                return;

            var dialog = new VistaOpenFileDialog();

            if (!string.IsNullOrWhiteSpace(setting.UserValue))
                dialog.FileName = setting.UserValue.GetFullPath();

            if (dialog.ShowDialog() != true)
                return;

            setting.UserValue = dialog.FileName;
        }

        #endregion

        #region Properties

        [Bindable]
        public ICommand SelectFileCmd { get; private set; }

        #endregion
    }
}
