using System.Windows.Controls;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Mock.Views
{
    /// <summary>
    /// A view with a constructor dependency: proves views are built through ActivatorUtilities.
    /// </summary>
    public sealed class InjectedView : UserControl
    {
        public InjectedView(ViewDependency dependency)
        {
            Dependency = dependency;
        }

        public ViewDependency Dependency { get; }
    }
}
