using OutWit.Common.Platform.Models.SystemHealth;

namespace OutWit.Common.Platform.Interfaces
{
    /// <summary>
    /// Collects a reusable snapshot of current machine health.
    /// </summary>
    public interface ISystemHealthProvider
    {
        /// <summary>
        /// Collects the current machine health snapshot.
        /// </summary>
        Task<SystemHealthSnapshot> CollectAsync();
    }
}
