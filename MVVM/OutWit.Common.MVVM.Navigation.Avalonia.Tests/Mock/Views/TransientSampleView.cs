using Avalonia.Controls;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock.Views
{
    public sealed class TransientSampleView : UserControl
    {
        public TransientSampleView()
        {
            Content = new TextBlock { Text = "Transient" };
        }
    }
}
