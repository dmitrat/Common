using MemoryPack;
using OutWit.Common.Enums;

namespace OutWit.Common.MemoryPack.Formatters
{
    public class StringEnumMemoryPackFormatter<T> : MemoryPackFormatter<T> where T : StringEnum<T>
    {
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value)
        {
            if (value is null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteString(value.Value);
        }

        public override void Deserialize(ref MemoryPackReader reader, scoped ref T value)
        {
            if (reader.PeekIsNull())
            {
                reader.Advance(1);
                value = null;
                return;
            }

            var str = reader.ReadString();

            if (str == null)
            {
                value = null;
                return;
            }

            value = StringEnum<T>.Parse(str);
        }
    }
}
