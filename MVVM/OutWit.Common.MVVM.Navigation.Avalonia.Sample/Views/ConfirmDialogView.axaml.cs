using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Sample.Views
{
    public partial class ConfirmDialogView : UserControl
    {
        public ConfirmDialogView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
