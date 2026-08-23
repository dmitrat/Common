using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Sample.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
