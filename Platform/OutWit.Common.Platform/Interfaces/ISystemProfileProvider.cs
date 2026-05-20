using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Interfaces
{
    /// <summary>
    /// Collects a reusable system profile for the current machine.
    /// </summary>
    public interface ISystemProfileProvider
    {
        /// <summary>
        /// Collects the current machine system profile.
        /// </summary>
        Task<SystemProfile> CollectAsync();
    }
}
