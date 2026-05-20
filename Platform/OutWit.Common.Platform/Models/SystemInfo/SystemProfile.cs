using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.SystemInfo
{
    /// <summary>
    /// Describes the machine system profile in a platform-neutral form.
    /// </summary>
    public sealed class SystemProfile : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SystemProfile other)
                return false;

            return Os.Is(other.Os, tolerance)
                   && Cpu.Is(other.Cpu, tolerance)
                   && Memory.Is(other.Memory, tolerance)
                   && TempStorage.Is(other.TempStorage, tolerance)
                   && Gpus.Count.Is(other.Gpus.Count)
                   && Gpus.Zip(other.Gpus).All(me => me.First.Is(me.Second, tolerance));
        }

        public override SystemProfile Clone()
        {
            return new SystemProfile
            {
                Os = Os.Clone(),
                Cpu = Cpu.Clone(),
                Memory = Memory.Clone(),
                Gpus = Gpus.Select(me => me.Clone()).ToArray(),
                TempStorage = TempStorage.Clone()
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Operating system information.
        /// </summary>
        public SystemOsInfo Os { get; init; } = new();

        /// <summary>
        /// CPU information.
        /// </summary>
        public SystemCpuInfo Cpu { get; init; } = new();

        /// <summary>
        /// Memory information.
        /// </summary>
        public SystemMemoryInfo Memory { get; init; } = new();

        /// <summary>
        /// GPU information.
        /// </summary>
        public IReadOnlyList<SystemGpuInfo> Gpus { get; init; } = [];

        /// <summary>
        /// Temporary-storage information.
        /// </summary>
        public SystemStorageInfo TempStorage { get; init; } = new();

        #endregion
    }
}
