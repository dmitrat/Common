namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// One call seen by <see cref="BlockDeviceProbe"/>.
    /// </summary>
    internal sealed record BlockDeviceRequest(BlockDeviceOperation Operation, long Sector, int Count);
}
