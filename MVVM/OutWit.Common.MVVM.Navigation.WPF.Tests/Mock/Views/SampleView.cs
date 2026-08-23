using System.Windows.Controls;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Mock.Views
{
    public sealed class SampleView : UserControl
    {
        public SampleView()
        {
            Content = new TextBlock { Text = "Sample" };
        }
    }
}
