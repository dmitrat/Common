using System;
using MemoryPack;

namespace OutWit.Common.Settings.Formatters
{
    /// <summary>
    /// MemoryPack formatter for boxed values (primarily enums).
    /// Stores as {AssemblyQualifiedTypeName, StringValue} pair.
    /// On deserialize, restores enums via <see cref="Enum.Parse(Type, string)"/>.
    /// </summary>
    public sealed class SettingsObjectFormatter : MemoryPackFormatter<object>
    {
        /// <inheritdoc />
        public override void Serialize<TBufferWriter>(
            ref MemoryPackWriter<TBufferWriter> writer,
            scoped ref object? value)
        {
            if (value == null)
            {
                writer.WriteNullObjectHeader();
                return;
            }

            writer.WriteObjectHeader(2);
            writer.WriteString(value.GetType().AssemblyQualifiedName);
            writer.WriteString(value.ToString());
        }

        /// <inheritdoc />
        public override void Deserialize(
            ref MemoryPackReader reader,
            scoped ref object? value)
        {
            if (!reader.TryReadObjectHeader(out var count))
            {
                value = null;
                return;
            }

            var typeName = count > 0 ? reader.ReadString() : null;
            var stringValue = count > 1 ? reader.ReadString() : null;

            if (typeName == null || stringValue == null)
            {
                value = stringValue;
                return;
            }

            var type = Type.GetType(typeName);
            if (type != null && type.IsEnum && Enum.TryParse(type, stringValue, out var result))
            {
                value = result;
                return;
            }

            value = stringValue;
        }
    }
}
