using System;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// Builds a licensing runtime without a container.
    /// <para>
    /// Desktop applications and host add-ins compose by hand — there is no
    /// <c>IServiceCollection</c> in an <c>App.axaml.cs</c> and none inside a CAD
    /// host's plugin loader — and until now <c>AddLicensing</c> was the only
    /// path that performed the initial evaluation. That made dependency
    /// injection a prerequisite for a correct startup rather than a convenience.
    /// </para>
    /// </summary>
    public static class Licensing
    {
        #region Functions

        /// <summary>
        /// Builds a service from the supplied configuration and evaluates the
        /// installed licences once before returning.
        /// </summary>
        /// <example>
        /// <code>
        /// var licensing = Licensing.Create(options => options
        ///     .ForProduct("WitSweep", SweepVersionInfo.Version)
        ///     .WithKeyRing(LicenseKeyRing.FromJson(EmbeddedRing()))
        ///     .WithBinding(new LicenseBindingProviderMachine())
        ///     .WithStore(new LicenseStoreFile(licenceDirectory))
        ///     .WithDemo(TimeSpan.FromDays(30))
        ///     .WithPeriodicReload(TimeSpan.FromHours(6)));
        /// </code>
        /// </example>
        /// <exception cref="InvalidOperationException">No product was named.</exception>
        public static ILicenseService Create(Action<LicensingOptions> configure)
        {
            var options = new LicensingOptions();
            configure(options);

            return Create(options);
        }

        /// <summary>Builds a service from options that have already been assembled.</summary>
        /// <exception cref="InvalidOperationException">No product was named.</exception>
        public static ILicenseService Create(LicensingOptions options)
        {
            return CreateService(options);
        }

        #endregion

        #region Tools

        /// <summary>
        /// The concrete construction, shared with <c>AddLicensing</c>, which
        /// needs the implementation type so the container can dispose it — the
        /// service now owns a timer.
        /// </summary>
        internal static LicenseService CreateService(LicensingOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.Product))
                throw new InvalidOperationException("Licensing requires a product — call ForProduct(...).");

            var service = new LicenseService(options);

            // Evaluated here rather than lazily: a product that discovers its
            // licence state on first use discovers it at an arbitrary moment,
            // and a startup banner that renders before the state settles is a
            // bug nobody reproduces reliably.
            //
            // Blocking here is only safe because every await inside this package
            // uses ConfigureAwait(false). A desktop host calls this from its UI
            // thread; one captured synchronization context anywhere below would
            // deadlock the application at startup — the process alive, no window
            // ever shown. Do not remove those ConfigureAwait calls.
            service.ReloadAsync().GetAwaiter().GetResult();

            return service;
        }

        #endregion
    }
}
