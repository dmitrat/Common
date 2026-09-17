using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// Byte-addressed access to block devices.
    /// </summary>
    public static class BlockDeviceExtensions
    {
        #region Functions

        /// <summary>
        /// Reads bytes that need not start or end on a sector boundary.
        /// </summary>
        /// <remarks>
        /// Whole sectors in the middle of the range are read straight into
        /// <paramref name="destination"/> in one request; only a partial sector at either
        /// end goes through a buffer of its own.
        /// </remarks>
        /// <param name="device">The device to read.</param>
        /// <param name="offset">The first byte to read, counted from the start of the device.</param>
        /// <param name="destination">Receives the bytes.</param>
        /// <param name="cancellationToken">Cancels the read.</param>
        /// <returns>A task that completes when <paramref name="destination"/> is filled.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The range lies outside the device.</exception>
        public static async ValueTask ReadBytesAsync(this IBlockDevice device, long offset, Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            CheckRange(device, offset, destination.Length);
            int sectorSize = device.SectorSize;

            while (!destination.IsEmpty)
            {
                long sector = offset / sectorSize;
                int within = (int)(offset % sectorSize);
                int count;

                if (within == 0 && destination.Length >= sectorSize)
                {
                    count = destination.Length / sectorSize * sectorSize;
                    await device.ReadAsync(sector, destination[..count], cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    count = Math.Min(sectorSize - within, destination.Length);
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(sectorSize);
                    try
                    {
                        await device.ReadAsync(sector, buffer.AsMemory(0, sectorSize), cancellationToken).ConfigureAwait(false);
                        buffer.AsMemory(within, count).CopyTo(destination);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                    }
                }

                destination = destination[count..];
                offset += count;
            }
        }

        /// <summary>
        /// Writes bytes that need not start or end on a sector boundary.
        /// </summary>
        /// <remarks>
        /// Whole sectors in the middle of the range are written in one request; a partial
        /// sector at either end is read, changed and written back, so the bytes around the
        /// range are kept.
        /// </remarks>
        /// <param name="device">The device to write.</param>
        /// <param name="offset">The first byte to write, counted from the start of the device.</param>
        /// <param name="source">The bytes.</param>
        /// <param name="cancellationToken">Cancels the write.</param>
        /// <returns>A task that completes when the device has accepted the bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The range lies outside the device.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        public static async ValueTask WriteBytesAsync(this IBlockDevice device, long offset, ReadOnlyMemory<byte> source,
            CancellationToken cancellationToken = default)
        {
            CheckRange(device, offset, source.Length);
            int sectorSize = device.SectorSize;

            while (!source.IsEmpty)
            {
                long sector = offset / sectorSize;
                int within = (int)(offset % sectorSize);
                int count;

                if (within == 0 && source.Length >= sectorSize)
                {
                    count = source.Length / sectorSize * sectorSize;
                    await device.WriteAsync(sector, source[..count], cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    count = Math.Min(sectorSize - within, source.Length);
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(sectorSize);
                    try
                    {
                        var whole = buffer.AsMemory(0, sectorSize);
                        await device.ReadAsync(sector, whole, cancellationToken).ConfigureAwait(false);
                        source[..count].CopyTo(whole[within..]);
                        await device.WriteAsync(sector, whole, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
                    }
                }

                source = source[count..];
                offset += count;
            }
        }

        private static void CheckRange(IBlockDevice device, long offset, int length)
        {
            ArgumentNullException.ThrowIfNull(device);

            long size = device.SectorCount * device.SectorSize;
            if (offset < 0 || offset > size - length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset,
                    $"Bytes {offset} to {offset + length - 1} lie outside the device of {size} bytes.");
        }

        #endregion
    }
}
