using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// One thing <see cref="FatChecker"/> found wrong.
    /// </summary>
    public sealed class FatProblem : ModelBase
    {
        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FatProblem other)
                return false;

            return Kind.Is(other.Kind)
                   && Path.Is(other.Path)
                   && Cluster.Is(other.Cluster)
                   && Message.Is(other.Message);
        }

        /// <inheritdoc />
        public override FatProblem Clone()
        {
            return new FatProblem
            {
                Kind = Kind,
                Path = Path,
                Cluster = Cluster,
                Message = Message
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// What kind of problem it is.
        /// </summary>
        [ToString]
        public FatProblemKind Kind { get; init; }

        /// <summary>
        /// The file or directory it concerns, or <c>null</c> when it concerns the volume.
        /// </summary>
        [ToString]
        public string? Path { get; init; }

        /// <summary>
        /// The first cluster it concerns, or <c>null</c>.
        /// </summary>
        public uint? Cluster { get; init; }

        /// <summary>
        /// What is wrong, in words.
        /// </summary>
        public required string Message { get; init; }

        #endregion
    }
}
