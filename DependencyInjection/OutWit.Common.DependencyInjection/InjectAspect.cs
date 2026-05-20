using System;
using System.Linq;
using AspectInjector.Broker;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.DependencyInjection.Internal;

namespace OutWit.Common.DependencyInjection
{
    /// <summary>
    /// AspectInjector aspect powering property-based dependency injection.
    /// <para>
    /// Mixes <see cref="IInjectable"/> into the target class and intercepts getters
    /// of <see cref="InjectAttribute"/>-annotated properties. The
    /// <see cref="IServiceProvider"/> is discovered in this order:
    /// </para>
    /// <list type="number">
    /// <item><see cref="IInjectable.ServiceProvider"/> if set explicitly
    /// (e.g. via <see cref="InjectableExtensions.InitInject"/> or
    /// <see cref="ServiceCollectionExtensions"/>).</item>
    /// <item>The first instance field of type <see cref="IServiceProvider"/>
    /// found by walking the type hierarchy.</item>
    /// </list>
    /// <para>
    /// If neither is available the getter is passive — it returns the current backing
    /// field value as-is, which makes the host class usable without DI (e.g. in tests).
    /// </para>
    /// </summary>
    [Mixin(typeof(IInjectable))]
    [Aspect(Scope.PerInstance)]
    public class InjectAspect : IInjectable
    {
        #region IInjectable

        /// <inheritdoc />
        public IServiceProvider? ServiceProvider { get; set; }

        #endregion

        #region Advice

        /// <summary>
        /// Intercepts getters on <see cref="InjectAttribute"/> properties and resolves
        /// the service from <see cref="ServiceProvider"/> (or a discovered instance
        /// field) according to the property's <see cref="InjectMode"/>.
        /// </summary>
        [Advice(Kind.Around, Targets = Target.AnyAccess | Target.Getter)]
        public object? AroundGetter(
            [Argument(Source.Instance)] object source,
            [Argument(Source.Name)] string propName,
            [Argument(Source.Target)] Func<object[], object> getter,
            [Argument(Source.Arguments)] object[] args,
            [Argument(Source.Triggers)] Attribute[] triggers)
        {
            var current = getter(args);

            // Only act on properties annotated with [Inject].
            if (triggers.OfType<InjectAttribute>().SingleOrDefault() == null)
                return current;

            var map = InjectPropertyMetadata.GetMap(source.GetType());
            if (!map.TryGetValue(propName, out var meta))
                return current;

            // Honor an already-assigned backing field for Cached and Scoped modes:
            // both write back into the field after first resolve, so a non-null value
            // means "already resolved, return as-is". Transient is opt-in to "always
            // resolve fresh" and intentionally does not cache.
            if (current != null && meta.Mode != InjectMode.Transient)
                return current;

            var sp = ServiceProvider ?? ServiceProviderLocator.Find(source);
            if (sp == null)
                return current;

            return meta.Mode switch
            {
                InjectMode.Transient => ResolveTransient(sp, meta),
                InjectMode.Scoped    => ResolveScoped(source, propName, sp, meta),
                _                    => ResolveCached(source, sp, meta),
            };
        }

        /// <summary>
        /// Disposes any scopes opened for <see cref="InjectMode.Scoped"/> properties when
        /// the owner's <c>Dispose</c> / <c>DisposeAsync</c> is invoked.
        /// </summary>
        [Advice(Kind.Before, Targets = Target.Method)]
        public void BeforeMethod(
            [Argument(Source.Instance)] object source,
            [Argument(Source.Name)] string methodName)
        {
            if (methodName != nameof(IDisposable.Dispose)
                && methodName != nameof(IAsyncDisposable.DisposeAsync))
                return;

            InjectScopeCache.DisposeAll(source);
        }

        #endregion

        #region Tools

        private static object? ResolveTransient(IServiceProvider sp, InjectPropertyMetadata meta)
        {
            return meta.IsRequired
                ? sp.GetRequiredService(meta.ServiceType)
                : sp.GetService(meta.ServiceType);
        }

        private static object? ResolveScoped(object source, string propName, IServiceProvider sp, InjectPropertyMetadata meta)
        {
            var scope = InjectScopeCache.GetOrCreate(source, propName, sp);

            var service = meta.IsRequired
                ? scope.ServiceProvider.GetRequiredService(meta.ServiceType)
                : scope.ServiceProvider.GetService(meta.ServiceType);

            if (service != null)
                meta.Property.SetValue(source, service);

            return service;
        }

        private static object? ResolveCached(object source, IServiceProvider sp, InjectPropertyMetadata meta)
        {
            var service = meta.IsRequired
                ? sp.GetRequiredService(meta.ServiceType)
                : sp.GetService(meta.ServiceType);

            if (service != null)
                meta.Property.SetValue(source, service);

            return service;
        }

        #endregion
    }
}
