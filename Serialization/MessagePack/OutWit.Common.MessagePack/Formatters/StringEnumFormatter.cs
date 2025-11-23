using MessagePack;
using MessagePack.Formatters;
using OutWit.Common.Enums;

namespace OutWit.Common.MessagePack.Formatters
{
    /// <summary>
    /// Generic MessagePack formatter for StringEnum.
    /// Serializes the enum as a plain string.
    /// </summary>
    public class StringEnumFormatter<T> : IMessagePackFormatter<T> where T : StringEnum<T>
    {
        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            // Write the underlying string value directly
            writer.Write(value.Value);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null!;
            }

            // Read string and parse using existing logic
            var str = reader.ReadString();

            if (str == null) return null!;

            return StringEnum<T>.Parse(str);
        }
    }
}
