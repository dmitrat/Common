namespace OutWit.Common.Settings.Tests.Utils
{
    /// <summary>
    /// Service interface for <see cref="TestSettings"/> to test interface-based DI registration.
    /// </summary>
    public interface ITestSettings
    {
        string UserName { get; set; }
        bool DarkMode { get; set; }
        int MaxRetries { get; set; }
        string AppVersion { get; set; }
    }
}
