using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace OutWit.Common.DependencyInjection.Internal
{
    /// <summary>
    /// Per-instance cache of <see cref="IServiceScope"/> objects, keyed by property name.
    /// Backs the <see cref="InjectMode.Scoped"/> resolution mode in <see cref="InjectAspect"/>:
    /// each scoped property gets its own dedicated child scope created on first access,
    /// shared by subsequent reads, and disposed when the owner is disposed.
    /// </summary>
    internal static class InjectScopeCache
    {
        #region Constants

        private static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, IServiceScope>> CACHE = new();

        #endregion

        #region Functions

        /// <summary>
        /// Returns the existing scope for <paramref name="propertyName"/> on <paramref name="owner"/>,
        /// or creates a new one from <paramref name="serviceProvider"/>.
        /// </summary>
        public static IServiceScope GetOrCreate(object owner, string propertyName, IServiceProvider serviceProvider)
        {
            var scopes = CACHE.GetValue(owner, _ => new ConcurrentDictionary<string, IServiceScope>());
            return scopes.GetOrAdd(propertyName, _ => serviceProvider.CreateScope());
        }

        /// <summary>
        /// Disposes all scopes owned by <paramref name="owner"/> and removes the cache entry.
        /// Called from the aspect's Dispose / DisposeAsync interception.
        /// </summary>
        public static void DisposeAll(object owner)
        {
            if (!CACHE.TryGetValue(owner, out var scopes))
                return;

            foreach (var pair in scopes)
                pair.Value.Dispose();

            CACHE.Remove(owner);
        }

        #endregion
    }
}
