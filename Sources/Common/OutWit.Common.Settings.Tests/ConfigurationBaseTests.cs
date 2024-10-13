using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Utils;
using OutWit.Common.Configuration;
using OutWit.Common.Settings.Tests.Properties;

namespace OutWit.Common.Settings.Tests
{
    [TestFixture]
    public class ConfigurationBaseTests
    {
        [Test]
        public void ConstructorTest()
        {
            var config = new ConfigurationManager(Assembly.GetExecutingAssembly(), true);
            Assert.That(config.Default, Is.Not.Null);
            Assert.That(File.Exists(config.Default.ConfigurationPath), Is.True);

            Assert.That(config.User, Is.Not.Null);
            Assert.That(File.Exists(config.User.ConfigurationPath), Is.True);

            Assert.That(config.User.ConfigurationPath, Is.Not.EqualTo(config.Default.ConfigurationPath));

            File.Delete(config.User.ConfigurationPath);

            config = new ConfigurationManager(Assembly.GetExecutingAssembly(), true);
            Assert.That(config.Default, Is.Not.Null);
            Assert.That(File.Exists(config.Default.ConfigurationPath), Is.True);

            Assert.That(config.User, Is.Not.Null);
            Assert.That(File.Exists(config.User.ConfigurationPath), Is.True);

            Assert.That(config.User.ConfigurationPath, Is.Not.EqualTo(config.Default.ConfigurationPath));

        }
    }
}
