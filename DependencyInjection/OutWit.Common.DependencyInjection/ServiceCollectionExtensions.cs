using System;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Common.DependencyInjection
{
    /// <summary>
    /// Registration helpers that combine standard
    /// <see cref="IServiceCollection"/> registration with eager
    /// <see cref="InjectAttribute"/> property resolution at instance creation time.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Singleton

        /// <summary>
        /// Registers a singleton service and eagerly resolves its
        /// <see cref="InjectAttribute"/> properties at creation time.
        /// </summary>
        public static IServiceCollection AddSingletonWithInject<TService>(
            this IServiceCollection services)
            where TService : class
        {
            services.AddSingleton(sp => CreateAndInject<TService>(sp));
            return services;
        }

        /// <summary>
        /// Registers a singleton interface/implementation pair and eagerly resolves
        /// the implementation's <see cref="InjectAttribute"/> properties.
        /// </summary>
        public static IServiceCollection AddSingletonWithInject<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddSingleton<TService>(sp => CreateAndInject<TImplementation>(sp));
            return services;
        }

        #endregion

        #region Scoped

        /// <summary>
        /// Registers a scoped service and eagerly resolves its
        /// <see cref="InjectAttribute"/> properties.
        /// </summary>
        public static IServiceCollection AddScopedWithInject<TService>(
            this IServiceCollection services)
            where TService : class
        {
            services.AddScoped(sp => CreateAndInject<TService>(sp));
            return services;
        }

        /// <summary>
        /// Registers a scoped interface/implementation pair and eagerly resolves
        /// the implementation's <see cref="InjectAttribute"/> properties.
        /// </summary>
        public static IServiceCollection AddScopedWithInject<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddScoped<TService>(sp => CreateAndInject<TImplementation>(sp));
            return services;
        }

        #endregion

        #region Transient

        /// <summary>
        /// Registers a transient service and eagerly resolves its
        /// <see cref="InjectAttribute"/> properties for every newly created instance.
        /// </summary>
        public static IServiceCollection AddTransientWithInject<TService>(
            this IServiceCollection services)
            where TService : class
        {
            services.AddTransient(sp => CreateAndInject<TService>(sp));
            return services;
        }

        /// <summary>
        /// Registers a transient interface/implementation pair and eagerly resolves
        /// the implementation's <see cref="InjectAttribute"/> properties.
        /// </summary>
        public static IServiceCollection AddTransientWithInject<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddTransient<TService>(sp => CreateAndInject<TImplementation>(sp));
            return services;
        }

        #endregion

        #region Tools

        private static T CreateAndInject<T>(IServiceProvider serviceProvider)
            where T : class
        {
            var instance = ActivatorUtilities.CreateInstance<T>(serviceProvider);

            // Wire the mixin so any subsequent lazy getter access still works.
            if (instance is IInjectable injectable)
                injectable.ServiceProvider = serviceProvider;

            // Resolve everything now so the instance is fully populated on return.
            PropertyInjector.Inject(instance, serviceProvider);

            return instance;
        }

        #endregion
    }
}
