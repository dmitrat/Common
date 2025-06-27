using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Services.Interfaces;

namespace OutWit.Common.MEF.Interfaces
{
    public interface IModule
    {
        void Initialize(IServiceContainer container);
    }
}
