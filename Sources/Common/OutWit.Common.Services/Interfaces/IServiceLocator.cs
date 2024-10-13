using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Services.Interfaces
{
    public interface IServiceLocator : IServiceContainer, IDisposable
    {
        void Register<TService>(TService instance);
    }
}
