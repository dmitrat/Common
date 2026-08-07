using System;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// Wires licensing into a host's service collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Functions

        /// <summary>
        /// Registers <see cref="ILicenseService"/> as a singleton and evaluates
        /// the installed licences once during registration, so the first caller
        /// sees a settled state rather than a "not loaded yet" one.
        /// </summary>
        /// <example>
        /// <code>
        /// services.AddLicensing(options => options
        ///     .ForProduct("WitSweep", ThisAssembly.Version)
        ///     .WithKeyRing(LicenseKeyRing.FromJson(EmbeddedRing()))
        ///     .WithBinding(new LicenseBindingProviderMachine())
        ///     .WithStore(new LicenseStoreFile(licenceDirectory))
        ///     .WithDemo(TimeSpan.FromDays(30), demo => demo.Limit("maxVariants", 8))
        ///     .Declares(v => v
        ///         .Feature("format.nas", "Nastran decks")
        ///         .Limit("maxVariants", "Variants per sweep", 64)));
        /// </code>
        /// </example>
        public static IServiceCollection AddLicensing(this IServiceCollection services, Action<LicensingOptions> configure)
        {
            var options = new LicensingOptions();
            configure(options);

            if (string.IsNullOrWhiteSpace(options.Product))
                throw new InvalidOperationException("AddLicensing requires a product — call ForProduct(...).");

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

            services.AddSingleton(options);
            services.AddSingleton<ILicenseService>(service);

            return services;
        }

        #endregion
    }
}
