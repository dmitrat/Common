using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.MemoryPack.Attributes;
using OutWit.Common.MemoryPack.Formatters;
using OutWit.Common.Values;

namespace OutWit.Common.MemoryPack.Tests.Utils
{
    [MemoryPackable]
    public partial class MockData : ModelBase
    {
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is MockData data))
                return false;

            return Text.Is(data.Text) &&
                   Value.Is(data.Value, tolerance) 
                   && Date.Is(data.Date)
                   && Color.Is(data.Color)
                   && Type?.Equals(data.Type) == true;
        }

        public override ModelBase Clone()
        {
            return new MockData {Text = Text, Value = Value, Type = Type};
        }

        public string? Text { get; set; }

        public double Value { get; set; }

        public DateTimeOffset? Date { get; set; }

        [StringEnumFormatterAttribute<ColorEnum>]
        public ColorEnum? Color { get; set; }
        
        public Type? Type { get; set; }
    }
}
