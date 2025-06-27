using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Tests.Mock
{
    public class NonComparable
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is NonComparable other &&
                   Id == other.Id &&
                   Name == other.Name;
        }

        public override int GetHashCode() => HashCode.Combine(Id, Name);
    }
}
