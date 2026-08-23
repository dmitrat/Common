namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A third aware view model type. Its log name is "AwareThird".
    /// </summary>
    public sealed class AwareThirdViewModel : AwareViewModel
    {
        public AwareThirdViewModel(CallLog log, StalledTargetGateHolder? gates = null)
            : base(log, gates)
        {
        }
    }
}
