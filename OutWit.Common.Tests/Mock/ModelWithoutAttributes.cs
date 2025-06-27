using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Abstract;

namespace OutWit.Common.Tests.Mock
{
    public class ModelWithoutAttributes : ModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override ModelBase Clone() => throw new NotImplementedException();
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE) => throw new NotImplementedException();
    }
}
