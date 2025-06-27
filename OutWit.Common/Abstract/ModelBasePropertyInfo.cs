using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OutWit.Common.Abstract
{
    internal class ModelBasePropertyInfo
    {
        public PropertyInfo Property { get; set; }
        
        public string Name { get; set; }

        public string Format { get; set; }
    }
}
