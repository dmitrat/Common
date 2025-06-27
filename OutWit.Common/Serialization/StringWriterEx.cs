using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OutWit.Common.Serialization
{
    public class StringWriterEx : StringWriter
    {
        public StringWriterEx(Encoding encoding)
        {
            Encoding = encoding;
        }

        public override Encoding Encoding { get; }
    }
}
