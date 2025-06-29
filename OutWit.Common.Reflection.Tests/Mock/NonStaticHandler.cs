using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Reflection.Tests.Mock
{
    internal class NonStaticHandler
    {
        public void HandleInstance<T>(T sender, string eventName, object[] parameters) where T : class
        {
            // This method is non-static and should cause an exception
        }
    }
}
