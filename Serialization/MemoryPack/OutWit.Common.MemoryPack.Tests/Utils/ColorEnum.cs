using OutWit.Common.Enums;

namespace OutWit.Common.MemoryPack.Tests.Utils
{
    public sealed class ColorEnum : StringEnum<ColorEnum>
    {
        public static readonly ColorEnum Red = new ColorEnum("RED");
        public static readonly ColorEnum Green = new ColorEnum("GREEN");
        public static readonly ColorEnum Blue = new ColorEnum("BLUE");

        private ColorEnum(string value) : base(value) { }
    }
}
