using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// Wires licensing into a host's service collection.
    /// <para>
    /// Every registration here is idempotent and lands on one shared
    /// <see cref="LicensingOptions"/>. In a modular host that matters: two
    /// modules that both want licensing would otherwise produce two services,
    /// two timers and two stores writing the same sidecar.
    /// </para>
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Functions

        /// <summary>
        /// Registers <see cref="ILicenseService"/> as a singleton and evaluates
        /// the installed licences <b>during registration</b>, so the first
        /// caller sees a settled state rather than a "not loaded yet" one.
        /// <para>
        /// Use this when the configuration is self-contained. When any of it has
        /// to come out of the container — a store built from a directory
        /// provider, a key ring contributed by a module, a tenant slug read from
        /// configuration — use the overload that receives an
        /// <see cref="IServiceProvider"/>, because at this point there is no
        /// container to read from yet.
        /// </para>
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

            var service = Licensing.CreateService(options);

            services.TryAddSingleton(options);

            // Registered through a factory rather than as an instance so the
            // container takes ownership and disposes it. The service now holds a
            // timer, and a container that never disposes what it hands out
            // leaks one per host.
            services.TryAddSingleton(_ => service);
            services.TryAddSingleton<ILicenseService>(provider => provider.GetRequiredService<LicenseService>());

            return services;
        }

        /// <summary>
        /// Registers licensing whose configuration needs the container, and
        /// leaves construction to the first resolve.
        /// <para>
        /// The initial evaluation still happens before anyone sees the service —
        /// it moves from registration time to the factory, which runs once the
        /// container is fully built. That keeps the "no unsettled state"
        /// guarantee while letting the options depend on anything registered.
        /// </para>
        /// </summary>
        public static IServiceCollection AddLicensing(
            this IServiceCollection services,
            Action<IServiceProvider, LicensingOptions> configure)
        {
            return services
                .ConfigureLicensing(configure)
                .AddLicensingCore();
        }

        /// <summary>
        /// Contributes to the licensing configuration without deciding that
        /// licensing exists. Several modules may each add a piece — one the key
        /// ring, one the store, one the declared vocabulary — and they are
        /// applied in registration order to a single set of options.
        /// </summary>
        public static IServiceCollection ConfigureLicensing(
            this IServiceCollection services,
            Action<IServiceProvider, LicensingOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var registration = Registration(services);
            registration.Configurators.Add(configure);

            return services;
        }

        /// <summary>
        /// Registers the service over whatever <see cref="ConfigureLicensing"/>
        /// contributed. Safe to call more than once.
        /// </summary>
        public static IServiceCollection AddLicensingCore(this IServiceCollection services)
        {
            Registration(services);

            services.TryAddSingleton(provider => Build(provider));
            services.TryAddSingleton<ILicenseService>(provider => provider.GetRequiredService<LicenseService>());
            services.TryAddSingleton(provider => provider.GetRequiredService<LicenseService>().Options);

            return services;
        }

        #endregion

        #region Tools

        private static LicenseService Build(IServiceProvider provider)
        {
            var options = new LicensingOptions();

            foreach (var configure in provider.GetRequiredService<LicensingRegistration>().Configurators)
                configure(provider, options);

            return Licensing.CreateService(options);
        }

        /// <summary>
        /// Finds or creates the one accumulator every contribution lands on.
        /// Held in the collection itself rather than in a static, so two
        /// containers in one process — a test suite is the usual case — do not
        /// silently share a configuration.
        /// </summary>
        private static LicensingRegistration Registration(IServiceCollection services)
        {
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(LicensingRegistration) &&
                    descriptor.ImplementationInstance is LicensingRegistration existing)
                    return existing;
            }

            var registration = new LicensingRegistration();
            services.AddSingleton(registration);

            return registration;
        }

        #endregion
    }
}
