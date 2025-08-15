using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class EnumTests
    {
        [Test]
        public void ConstructorTest()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var assemblyPath = assembly.Location;
            var configPath = $"{assembly.Location}.config";

            var configuration1 = new Configuration.Configuration(assemblyPath);
            var configuration2 = new Configuration.Configuration(configPath);

        }
    }
}
