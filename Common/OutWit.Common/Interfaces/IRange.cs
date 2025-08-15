using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Interfaces
{
    public interface IRange<out TValue>
    {
        TValue From { get; }

        TValue To { get; }
    }
}
