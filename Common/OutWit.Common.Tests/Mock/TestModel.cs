using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Tests.Mock
{
    internal class TestModel : ModelBase
    {
        public override ModelBase Clone()
        {
            return new TestModel
            {
                Id = this.Id,
                Name = this.Name,
                Value = this.Value
            };
        }

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not TestModel other)
            {
                return false;
            }

            return Id.Is(other.Id) &&
                   Name.Is(other.Name) &&
                   Value.Is(other.Value, tolerance);
        }

        public int Id { get; set; }
        
        public string? Name { get; set; }
        
        public double Value { get; set; }
    }
}
