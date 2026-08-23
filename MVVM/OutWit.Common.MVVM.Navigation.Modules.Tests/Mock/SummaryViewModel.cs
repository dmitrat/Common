namespace OutWit.Common.MVVM.Navigation.Modules.Tests.Mock
{
    /// <summary>
    /// The view model a module routes to.
    /// </summary>
    public sealed class SummaryViewModel
    {
        public SummaryViewModel(SummaryService service)
        {
            Service = service;
        }

        public SummaryService Service { get; }
    }
}
