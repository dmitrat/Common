using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Configuration.Tests.Model
{
    public class SimpleSettings
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public bool UnboundValue { get; set; } = true;
    }

}
