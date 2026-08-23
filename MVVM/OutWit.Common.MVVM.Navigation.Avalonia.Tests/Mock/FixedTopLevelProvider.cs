using Avalonia.Controls;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock
{
    /// <summary>
    /// An ITopLevelProvider that returns the window a test created.
    /// </summary>
    public sealed class FixedTopLevelProvider : ITopLevelProvider
    {
        public FixedTopLevelProvider(TopLevel? topLevel)
        {
            TopLevel = topLevel;
        }

        public TopLevel? TopLevel { get; set; }

        public TopLevel? GetActive()
        {
            return TopLevel;
        }
    }
}
