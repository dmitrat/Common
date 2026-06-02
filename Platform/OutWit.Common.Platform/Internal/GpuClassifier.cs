using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Internal
{
    /// <summary>
    /// Vendor/name → <see cref="SystemGpuType"/> + <see cref="SystemGpuFeatures"/>
    /// classification heuristics. OS-agnostic — every probe that detects a GPU
    /// can run the same vendor strings through this to produce a consistent
    /// classification for the scheduler.
    /// </summary>
    internal static class GpuClassifier
    {
        #region Functions

        public static SystemGpuType DetectType(string vendor, string name)
        {
            var combined = $"{vendor} {name}".ToUpperInvariant();

            if (combined.Contains("INTEL") && (combined.Contains("UHD") || combined.Contains("HD GRAPHICS") || combined.Contains("IRIS")))
                return SystemGpuType.Integrated;

            if (combined.Contains("NVIDIA") || combined.Contains("AMD") || combined.Contains("RADEON"))
                return SystemGpuType.Discrete;

            if (combined.Contains("VIRTUAL") || combined.Contains("VMWARE") || combined.Contains("HYPER-V"))
                return SystemGpuType.Virtual;

            // Apple Silicon (M-series) and other Apple GPUs are integrated parts
            // that share the machine's unified memory.
            if (combined.Contains("APPLE"))
                return SystemGpuType.Integrated;

            return SystemGpuType.Unknown;
        }

        public static SystemGpuFeatures DetectFeatures(string vendor, string name)
        {
            var combined = $"{vendor} {name}".ToUpperInvariant();
            var features = SystemGpuFeatures.DirectX | SystemGpuFeatures.OpenGL;

            if (combined.Contains("NVIDIA"))
                return features | SystemGpuFeatures.Cuda | SystemGpuFeatures.Vulkan | SystemGpuFeatures.OpenCL;

            if (combined.Contains("AMD") || combined.Contains("RADEON"))
                return features | SystemGpuFeatures.Vulkan | SystemGpuFeatures.OpenCL | SystemGpuFeatures.ROCm;

            if (combined.Contains("INTEL"))
            {
                features |= SystemGpuFeatures.OpenCL;
                if (combined.Contains("ARC") || combined.Contains("IRIS XE"))
                    features |= SystemGpuFeatures.Vulkan;
            }

            // Apple Silicon and similar embedded GPUs on macOS / mobile.
            if (combined.Contains("APPLE"))
                return SystemGpuFeatures.Metal | SystemGpuFeatures.OpenGL;

            // Mobile GPU vendors common on Android.
            if (combined.Contains("ADRENO") || combined.Contains("QUALCOMM"))
                return SystemGpuFeatures.OpenGL | SystemGpuFeatures.Vulkan | SystemGpuFeatures.OpenCL;

            if (combined.Contains("MALI"))
                return SystemGpuFeatures.OpenGL | SystemGpuFeatures.Vulkan;

            if (combined.Contains("POWERVR"))
                return SystemGpuFeatures.OpenGL | SystemGpuFeatures.Vulkan;

            return features;
        }

        #endregion
    }
}
