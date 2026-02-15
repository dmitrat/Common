using MemoryPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.MemoryPack.Formatters
{
    internal sealed class DateTimeOffsetFormatter : MemoryPackFormatter<DateTimeOffset>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref DateTimeOffset value)
        {
            writer.WriteUnmanaged(value.Ticks, value.Offset.Ticks);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref DateTimeOffset value)
        {
            reader.ReadUnmanaged(out long ticks, out long offsetTicks);
            value = new DateTimeOffset(ticks, TimeSpan.FromTicks(offsetTicks));
        }
    }
}
