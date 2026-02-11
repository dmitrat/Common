using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace OutWit.Common.Settings.Database
{
    /// <summary>
    /// Custom model cache key factory that includes the scope in the cache key.
    /// Required because <see cref="SettingsScopedDbContext"/> produces different table
    /// configurations per <see cref="Configuration.SettingsScope"/>.
    /// Without this, EF Core would cache the first scope's model and reuse it for all scopes.
    /// </summary>
    internal sealed class SettingsScopedModelCacheKeyFactory : IModelCacheKeyFactory
    {
        #region IModelCacheKeyFactory

        /// <summary>
        /// Creates a cache key that includes the scope for <see cref="SettingsScopedDbContext"/>.
        /// For other context types, falls back to the default (type, designTime) key.
        /// </summary>
        public object Create(DbContext context, bool designTime)
        {
            if (context is SettingsScopedDbContext scopedContext)
                return (context.GetType(), scopedContext.ScopeKey, designTime);

            return (context.GetType(), designTime);
        }

        #endregion
    }
}
