using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OutWit.Common.MVVM.Navigation.Sample.Module.Avalonia
{
    public partial class AuditView : UserControl
    {
        public AuditView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
