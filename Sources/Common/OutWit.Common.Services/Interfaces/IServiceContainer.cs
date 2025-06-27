using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Services.Interfaces
{
    public interface IServiceContainer
    {
        TService Resolve<TService>();

        bool IsDisposed { get; }
    }
}
