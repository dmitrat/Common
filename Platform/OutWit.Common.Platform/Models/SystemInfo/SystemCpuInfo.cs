using System.Runtime.InteropServices;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.SystemInfo
{
    /// <summary>
    /// Describes CPU information in a reusable form.
    /// </summary>
    public sealed class SystemCpuInfo : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SystemCpuInfo other)
                return false;

            return Architecture.Equals(other.Architecture)
                   && LogicalCoreCount.Is(other.LogicalCoreCount)
                   && ModelName.Is(other.ModelName);
        }

        public override SystemCpuInfo Clone()
        {
            return new SystemCpuInfo
            {
                Architecture = Architecture,
                LogicalCoreCount = LogicalCoreCount,
                ModelName = ModelName
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Processor architecture.
        /// </summary>
        public Architecture Architecture { get; init; }

        /// <summary>
        /// Logical processor count.
        /// </summary>
        public int LogicalCoreCount { get; init; }

        /// <summary>
        /// Processor model name if available.
        /// </summary>
        public string ModelName { get; init; } = string.Empty;

        #endregion
    }
}
