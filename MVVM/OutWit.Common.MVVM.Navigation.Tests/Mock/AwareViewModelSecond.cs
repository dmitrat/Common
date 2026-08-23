namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// A second aware view model type, so two routes can target different instances.
    /// Its log name is "AwareSecond".
    /// </summary>
    public sealed class AwareSecondViewModel : AwareViewModel
    {
        public AwareSecondViewModel(CallLog log, StalledTargetGateHolder? gates = null)
            : base(log, gates)
        {
        }
    }
}
