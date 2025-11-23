using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Enums;

namespace OutWit.Common.Tests.Mock
{
    public sealed class ColorEnum : StringEnum<ColorEnum>
    {
        public static readonly ColorEnum Red = new ColorEnum("RED");
        public static readonly ColorEnum Green = new ColorEnum("GREEN");
        public static readonly ColorEnum Blue = new ColorEnum("BLUE");

        private ColorEnum(string value) : base(value) { }
    }
}
