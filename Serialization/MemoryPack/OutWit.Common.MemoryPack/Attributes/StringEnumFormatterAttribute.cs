using MemoryPack;
using OutWit.Common.Enums;
using OutWit.Common.MemoryPack.Formatters;

namespace OutWit.Common.MemoryPack.Attributes
{
    /// <summary>
    /// Custom attribute to easily apply StringEnum serialization in MemoryPack DTOs.
    /// </summary>
    /// <typeparam name="T">The concrete StringEnum type.</typeparam>
    public sealed class StringEnumFormatterAttribute<T> : MemoryPackCustomFormatterAttribute<T>
        where T : StringEnum<T>
    {
        public override IMemoryPackFormatter<T> GetFormatter()
        {
            return new StringEnumMemoryPackFormatter<T>();
        }
    }
}
