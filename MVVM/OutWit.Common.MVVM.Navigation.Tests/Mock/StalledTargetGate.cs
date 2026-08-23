using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// Plants a gate into the next <see cref="AwareViewModel"/> the navigation creates.
    /// The view model is built by the navigation itself, so a test cannot hand it the gate
    /// directly; instead the gate is published through the container's <see cref="CallLog"/>
    /// holder and every new AwareViewModel picks it up in its constructor.
    /// </summary>
    public static class StalledTargetGate
    {
        #region Functions

        public static void Install(IServiceProvider provider, TaskCompletionSource<bool> gate, bool stallOnNavigatedTo)
        {
            var holder = provider.GetRequiredService<StalledTargetGateHolder>();
            holder.Gate = gate;
            holder.StallOnNavigatedTo = stallOnNavigatedTo;
        }

        #endregion
    }

    /// <summary>
    /// Registered as a singleton by <see cref="NavigationTestHost"/>.
    /// </summary>
    public sealed class StalledTargetGateHolder
    {
        public TaskCompletionSource<bool>? Gate { get; set; }

        public bool StallOnNavigatedTo { get; set; }

        /// <summary>
        /// Hands the gate out once: the view model created next takes it.
        /// </summary>
        public TaskCompletionSource<bool>? Take(out bool stallOnNavigatedTo)
        {
            var gate = Gate;
            stallOnNavigatedTo = StallOnNavigatedTo;
            Gate = null;
            return gate;
        }
    }
}
