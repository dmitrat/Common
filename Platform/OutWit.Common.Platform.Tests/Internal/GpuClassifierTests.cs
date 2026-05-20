using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.SystemInfo;

namespace OutWit.Common.Platform.Tests.Internal
{
    /// <summary>
    /// Coverage for the shared vendor/name → GPU classification rules. These
    /// run on every platform probe that has GPU detection, so the rules need
    /// stable test coverage independent of any single probe.
    /// </summary>
    [TestFixture]
    public sealed class GpuClassifierTests
    {
        #region Type Detection

        [TestCase("NVIDIA Corporation", "GeForce RTX 4090", SystemGpuType.Discrete)]
        [TestCase("AMD", "Radeon RX 7900 XTX", SystemGpuType.Discrete)]
        [TestCase("Advanced Micro Devices", "AMD Radeon Graphics", SystemGpuType.Discrete)]
        [TestCase("Intel Corporation", "Intel UHD Graphics 630", SystemGpuType.Integrated)]
        [TestCase("Intel Corporation", "Intel Iris Plus Graphics", SystemGpuType.Integrated)]
        [TestCase("VMware Inc.", "VMware SVGA 3D", SystemGpuType.Virtual)]
        [TestCase("", "Hyper-V virtual GPU", SystemGpuType.Virtual)]
        [TestCase("Some Vendor", "Some Generic Adapter", SystemGpuType.Unknown)]
        public void DetectTypeClassifiesKnownGpusTest(string vendor, string name, SystemGpuType expected)
        {
            var type = GpuClassifier.DetectType(vendor, name);

            Assert.That(type, Is.EqualTo(expected));
        }

        #endregion

        #region Feature Detection

        [Test]
        public void NvidiaGpusAdvertiseCudaAndVulkanTest()
        {
            var features = GpuClassifier.DetectFeatures("NVIDIA Corporation", "RTX 4090");

            Assert.Multiple(() =>
            {
                Assert.That(features.HasFlag(SystemGpuFeatures.Cuda), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.Vulkan), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.OpenCL), Is.True);
            });
        }

        [Test]
        public void AmdGpusAdvertiseRocmAndVulkanTest()
        {
            var features = GpuClassifier.DetectFeatures("AMD", "Radeon RX 7900");

            Assert.Multiple(() =>
            {
                Assert.That(features.HasFlag(SystemGpuFeatures.ROCm), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.Vulkan), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.OpenCL), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.Cuda), Is.False);
            });
        }

        [Test]
        public void AppleGpusAdvertiseMetalAndNotCudaOrDirectXTest()
        {
            var features = GpuClassifier.DetectFeatures("Apple", "Apple M3 Max");

            Assert.Multiple(() =>
            {
                Assert.That(features.HasFlag(SystemGpuFeatures.Metal), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.Cuda), Is.False);
                Assert.That(features.HasFlag(SystemGpuFeatures.DirectX), Is.False);
            });
        }

        [Test]
        public void AdrenoGpusAdvertiseVulkanAndOpenClTest()
        {
            var features = GpuClassifier.DetectFeatures("Qualcomm", "Adreno 750");

            Assert.Multiple(() =>
            {
                Assert.That(features.HasFlag(SystemGpuFeatures.Vulkan), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.OpenCL), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.Cuda), Is.False);
            });
        }

        [Test]
        public void MaliGpusAdvertiseVulkanAndNotDirectXTest()
        {
            var features = GpuClassifier.DetectFeatures("ARM", "Mali-G715");

            Assert.Multiple(() =>
            {
                Assert.That(features.HasFlag(SystemGpuFeatures.Vulkan), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.OpenGL), Is.True);
                Assert.That(features.HasFlag(SystemGpuFeatures.DirectX), Is.False);
            });
        }

        #endregion
    }
}
