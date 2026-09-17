using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Passes every call to another device and records it; can fail reads, writes, flushes
    /// and disposals on demand.
    /// </summary>
    internal sealed class BlockDeviceProbe : IBlockDevice
    {
        #region Fields

        private readonly IBlockDevice m_inner;

        #endregion

        #region Constructors

        public BlockDeviceProbe(IBlockDevice inner)
        {
            m_inner = inner;
        }

        #endregion

        #region Functions

        public void Clear()
        {
            Requests.Clear();
        }

        public IEnumerable<BlockDeviceRequest> Of(BlockDeviceOperation operation)
        {
            return Requests.Where(request => request.Operation == operation);
        }

        #endregion

        #region IBlockDevice

        public ValueTask ReadAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int count = buffer.Length / SectorSize;
            Requests.Add(new BlockDeviceRequest(BlockDeviceOperation.Read, sector, count));
            if (FailRead?.Invoke(sector, count) == true)
                throw new IOException("Injected read failure.");

            return m_inner.ReadAsync(sector, buffer, cancellationToken);
        }

        public ValueTask WriteAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int count = buffer.Length / SectorSize;
            Requests.Add(new BlockDeviceRequest(BlockDeviceOperation.Write, sector, count));
            if (FailingWrites > 0 || FailWrite?.Invoke(sector, count) == true)
            {
                FailingWrites = Math.Max(0, FailingWrites - 1);
                throw new IOException("Injected write failure.");
            }

            return m_inner.WriteAsync(sector, buffer, cancellationToken);
        }

        public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            Requests.Add(new BlockDeviceRequest(BlockDeviceOperation.Flush, 0, 0));
            if (FailingFlushes > 0)
            {
                FailingFlushes--;
                throw new IOException("Injected flush failure.");
            }

            return m_inner.FlushAsync(cancellationToken);
        }

        #endregion

        #region IAsyncDisposable

        public ValueTask DisposeAsync()
        {
            if (FailingDisposals > 0)
            {
                FailingDisposals--;
                throw new IOException("Injected disposal failure.");
            }

            IsDisposed = true;
            return m_inner.DisposeAsync();
        }

        #endregion

        #region Properties

        public List<BlockDeviceRequest> Requests { get; } = new();

        public int FailingWrites { get; set; }

        public int FailingFlushes { get; set; }

        public int FailingDisposals { get; set; }

        /// <summary>
        /// Fails a read whose first sector and count it is given return true for.
        /// </summary>
        public Func<long, int, bool>? FailRead { get; set; }

        /// <summary>
        /// Fails a write whose first sector and count it is given return true for.
        /// </summary>
        public Func<long, int, bool>? FailWrite { get; set; }

        public bool IsDisposed { get; private set; }

        public long SectorsRead => Of(BlockDeviceOperation.Read).Sum(request => (long)request.Count);

        public int SectorSize => m_inner.SectorSize;

        public long SectorCount => m_inner.SectorCount;

        public bool IsReadOnly => m_inner.IsReadOnly;

        #endregion
    }
}
