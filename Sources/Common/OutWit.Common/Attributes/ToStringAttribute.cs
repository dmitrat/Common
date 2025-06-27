using System;

namespace OutWit.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ToStringAttribute : Attribute
    {
        public ToStringAttribute()
        {
            
        }

        public ToStringAttribute(string name)
        {
            Name = name;
        }
        
        public string Name { get; set; }

        public string Format { get; set; }
    }
}
