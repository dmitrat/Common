using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Fat.Model
{
    /// <summary>
    /// A partition that could not be used, and why.
    /// </summary>
    /// <remarks>
    /// Reported instead of thrown, so that one bad slot does not hide the good volumes on
    /// the same disk.
    /// </remarks>
    public sealed class FatPartitionProblem : ModelBase
    {
        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FatPartitionProblem other)
                return false;

            return Partition.Is(other.Partition, tolerance)
                   && Kind.Is(other.Kind)
                   && Message.Is(other.Message);
        }

        /// <inheritdoc />
        public override FatPartitionProblem Clone()
        {
            return new FatPartitionProblem
            {
                Partition = Partition.Clone(),
                Kind = Kind,
                Message = Message
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// The partition table slot.
        /// </summary>
        [ToString]
        public required MbrPartitionEntry Partition { get; init; }

        /// <summary>
        /// What is wrong with it.
        /// </summary>
        [ToString]
        public FatErrorKind Kind { get; init; }

        /// <summary>
        /// What is wrong with it, for a person.
        /// </summary>
        [ToString]
        public string Message { get; init; } = string.Empty;

        #endregion
    }
}
