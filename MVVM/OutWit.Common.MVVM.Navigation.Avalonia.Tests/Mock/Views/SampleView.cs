using System.Threading;
using Avalonia.Controls;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock.Views
{
    public sealed class SampleView : UserControl
    {
        private static int s_instances;

        public SampleView()
        {
            Id = Interlocked.Increment(ref s_instances);
            Content = new TextBlock { Text = "Sample" };
        }

        public int Id { get; }
    }
}
