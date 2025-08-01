using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Common.Plugins.Tests.Mock.Interfaces
{
    public interface ITestPlugin : IWitPlugin
    {
        string GetName();
    }
}
