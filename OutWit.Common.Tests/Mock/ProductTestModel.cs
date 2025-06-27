using OutWit.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;

namespace OutWit.Common.Tests.Mock
{
    public class ProductTestModel : ModelBase
    {
        [ToString(Name = "ID")]
        public int ProductId { get; set; }

        [ToString]
        public string? ProductName { get; set; }

        [ToString("Price", Format = "F2")] 
        public decimal UnitPrice { get; set; }

        [ToString(Format = "X8")] 
        public int ProductCode { get; set; }

        public int StockCount { get; set; }

        public override ModelBase Clone() => throw new NotImplementedException();
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE) => throw new NotImplementedException();
    }
}
