namespace OutWit.Common.Platform.Interfaces
{
    /// <summary>
    /// Provides a stable machine identity for the current device.
    /// </summary>
    public interface IMachineIdentityProvider
    {
        /// <summary>
        /// Returns a stable hashed machine identity.
        /// </summary>
        Task<string> GetMachineIdentityAsync();
    }
}
