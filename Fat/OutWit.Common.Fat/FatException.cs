using System;
using System.IO;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// A failure of the file system rather than of the device beneath it.
    /// </summary>
    /// <remarks>
    /// Device failures surface as the device's own exceptions; argument errors as the
    /// usual argument exceptions.
    /// </remarks>
    public sealed class FatException : IOException
    {
        #region Constructors

        /// <summary>
        /// Creates the exception.
        /// </summary>
        /// <param name="kind">What went wrong.</param>
        /// <param name="message">What went wrong, for a person.</param>
        public FatException(FatErrorKind kind, string message)
            : base(message)
        {
            Kind = kind;
        }

        /// <summary>
        /// Creates the exception with the failure that caused it.
        /// </summary>
        /// <param name="kind">What went wrong.</param>
        /// <param name="message">What went wrong, for a person.</param>
        /// <param name="innerException">The failure that caused this one.</param>
        public FatException(FatErrorKind kind, string message, Exception innerException)
            : base(message, innerException)
        {
            Kind = kind;
        }

        #endregion

        #region Properties

        /// <summary>
        /// What went wrong.
        /// </summary>
        public FatErrorKind Kind { get; }

        #endregion
    }
}
