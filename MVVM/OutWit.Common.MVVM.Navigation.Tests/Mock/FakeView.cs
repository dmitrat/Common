namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// What <see cref="FakeViewFactory"/> builds: an object that remembers its view model.
    /// </summary>
    public sealed class FakeView
    {
        public FakeView(object viewModel)
        {
            ViewModel = viewModel;
        }

        public object ViewModel { get; }
    }
}
