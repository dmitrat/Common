namespace OutWit.Common.Platform.Models.SystemInfo
{
    /// <summary>
    /// Flags describing supported GPU features.
    /// </summary>
    [Flags]
    public enum SystemGpuFeatures
    {
        None = 0,
        DirectX = 1,
        OpenGL = 2,
        OpenCL = 4,
        Vulkan = 8,
        Cuda = 16,
        Metal = 32,
        ROCm = 64
    }
}
