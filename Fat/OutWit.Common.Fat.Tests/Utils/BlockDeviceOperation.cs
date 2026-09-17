namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// The kind of a call recorded by <see cref="BlockDeviceProbe"/>.
    /// </summary>
    internal enum BlockDeviceOperation
    {
        Read,
        Write,
        Flush
    }
}
