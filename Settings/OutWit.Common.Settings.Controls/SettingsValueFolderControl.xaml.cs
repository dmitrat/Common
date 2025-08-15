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
    /// Interaction logic for SettingsValueFolderControl.xaml
    /// </summary>
    public partial class SettingsValueFolderControl : UserControl
    {
        #region DependencyProperties

        public static readonly DependencyProperty SelectFolderCmdProperty = BindingUtils.Register<SettingsValueFolderControl, ICommand>(nameof(SelectFolderCmd));

        #endregion

        #region Constructors

        public SettingsValueFolderControl()
        {
            InitializeComponent();
            InitCommands();
        }

        #endregion

        #region Initialization

        private void InitCommands()
        {
            SelectFolderCmd = new DelegateCommand(x => SelectFolder(x as SettingsValueFolder));
        }

        #endregion

        #region Functions

        private void SelectFolder(SettingsValueFolder setting)
        {
            if(setting == null)
                return;

            var dialog = new VistaFolderBrowserDialog();
            
            if (!string.IsNullOrWhiteSpace(setting.UserValue))
                dialog.SelectedPath = setting.UserValue.GetFullPath();

            if(dialog.ShowDialog() != true)
                return;

            setting.UserValue = dialog.SelectedPath;
        }

        #endregion

        #region Properties

        [Bindable]
        public ICommand SelectFolderCmd { get; private set; }

        #endregion

    }
}
