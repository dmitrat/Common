using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// A fixed number of equally sized sectors, read and written in batches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A call covers <c>buffer.Length / SectorSize</c> consecutive sectors starting at
    /// <c>sector</c>; the buffer length is always a whole number of sectors. Asking for
    /// many sectors at once is deliberate: a device reached over a slow link answers a
    /// batch in one round trip, and an implementation that has a limit per request
    /// splits the batch itself.
    /// </para>
    /// <para>
    /// Calls are not concurrent. The caller awaits each one before starting the next,
    /// so implementations need not synchronise.
    /// </para>
    /// </remarks>
    public interface IBlockDevice : IAsyncDisposable
    {
        #region Functions

        /// <summary>
        /// Reads consecutive sectors into <paramref name="buffer"/>.
        /// </summary>
        /// <param name="sector">The first sector to read.</param>
        /// <param name="buffer">Receives the sectors; its length is a multiple of <see cref="SectorSize"/>.</param>
        /// <param name="cancellationToken">Cancels the read.</param>
        /// <returns>A task that completes when the whole buffer is filled.</returns>
        /// <exception cref="ArgumentException">The buffer is not a whole number of sectors.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The sectors lie outside the device.</exception>
        /// <exception cref="ObjectDisposedException">The device is disposed.</exception>
        ValueTask ReadAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes consecutive sectors from <paramref name="buffer"/>.
        /// </summary>
        /// <param name="sector">The first sector to write.</param>
        /// <param name="buffer">The sectors to write; its length is a multiple of <see cref="SectorSize"/>.</param>
        /// <param name="cancellationToken">Cancels the write.</param>
        /// <returns>A task that completes when the device has accepted the whole buffer.</returns>
        /// <exception cref="ArgumentException">The buffer is not a whole number of sectors.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The sectors lie outside the device.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="ObjectDisposedException">The device is disposed.</exception>
        ValueTask WriteAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits everything written so far to the underlying medium.
        /// </summary>
        /// <param name="cancellationToken">Cancels the flush.</param>
        /// <returns>A task that completes when the written data is durable.</returns>
        /// <exception cref="ObjectDisposedException">The device is disposed.</exception>
        ValueTask FlushAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Properties

        /// <summary>
        /// The size of one sector in bytes: a power of two from 512 to 4096.
        /// </summary>
        int SectorSize { get; }

        /// <summary>
        /// The number of sectors on the device.
        /// </summary>
        long SectorCount { get; }

        /// <summary>
        /// Whether writes are refused.
        /// </summary>
        bool IsReadOnly { get; }

        #endregion
    }
}
