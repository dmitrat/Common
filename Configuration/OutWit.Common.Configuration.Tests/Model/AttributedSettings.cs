using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Configuration.Attributes;

namespace OutWit.Common.Configuration.Tests.Model
{
    public class AttributedSettings
    {
        [ConfigSection("Logging:LogLevel")]
        public string LogLevel { get; set; }

        [ConfigSection("ConnectionStrings")]
        public ConnectionDetails Database { get; set; }
    }

}
