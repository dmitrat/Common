using System.Windows;
using OutWit.Common.Settings.Samples.Wpf.ViewModels;

namespace OutWit.Common.Settings.Samples.Wpf.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ApplicationViewModel appVm)
                return;

            var window = new SettingsWindow
            {
                DataContext = appVm.Settings,
                Owner = this
            };

            window.ShowDialog();
        }
    }
}
