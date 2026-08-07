using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Storage;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// A desktop host calls <c>AddLicensing</c> from its UI thread, and that
    /// call blocks on an async pipeline. If any await inside the package
    /// captured the UI synchronization context, the application would deadlock
    /// at startup — process alive, no window ever shown, nothing in the log.
    /// <para>
    /// This regression happened, and it is invisible to every other test in this
    /// suite because a test runner has no synchronization context to capture.
    /// The fixture installs one that never runs anything, which is exactly what
    /// a blocked UI thread looks like.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class SynchronizationContextTests
    {
        #region Deadlock Tests

        [Test]
        public void AddLicensingDoesNotDeadlockUnderABlockedContextTest()
        {
            var completed = RunUnderBlockedContext(() =>
            {
                new ServiceCollection().AddLicensing(options => options
                    .ForProduct("SampleProduct", new Version(1, 0))
                    .WithBinding(new LicenseBindingProviderMachine())
                    .WithStore(new LicenseStoreMemory())
                    .WithDemo(TimeSpan.FromDays(30)));
            });

            Assert.That(completed, Is.True,
                "AddLicensing deadlocked — an await below it captured the synchronization context.");
        }

        [Test]
        public void ReloadDoesNotDeadlockUnderABlockedContextTest()
        {
            var completed = RunUnderBlockedContext(() =>
            {
                var options = new LicensingOptions()
                    .ForProduct("SampleProduct", new Version(1, 0))
                    .WithBinding(new LicenseBindingProviderComposite(
                        new LicenseBindingProviderMachine(),
                        new LicenseBindingProviderTenant("acme")))
                    .WithStore(new LicenseStoreMemory());

                new LicenseService(options).ReloadAsync().GetAwaiter().GetResult();
            });

            Assert.That(completed, Is.True);
        }

        #endregion

        #region Tools

        /// <summary>
        /// Runs <paramref name="action"/> on a thread whose synchronization
        /// context discards continuations instead of running them, and reports
        /// whether it finished. A captured context under a blocking call never
        /// resumes, so this returns false rather than hanging the suite.
        /// </summary>
        private static bool RunUnderBlockedContext(Action action)
        {
            var finished = new ManualResetEventSlim(false);

            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new BlockedSynchronizationContext());

                try
                {
                    action();
                }
                catch
                {
                    // A throw still means no deadlock, which is what is measured.
                }

                finished.Set();
            })
            {
                IsBackground = true
            };

            thread.Start();

            return finished.Wait(TimeSpan.FromSeconds(15));
        }

        /// <summary>Stands in for a UI thread that is busy blocking: posted work never runs.</summary>
        private sealed class BlockedSynchronizationContext : SynchronizationContext
        {
            public override void Post(SendOrPostCallback callback, object? state)
            {
            }

            public override void Send(SendOrPostCallback callback, object? state)
            {
            }
        }

        #endregion
    }
}
