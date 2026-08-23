using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Sample.Views
{
    public partial class StudiesView : UserControl
    {
        public StudiesView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
