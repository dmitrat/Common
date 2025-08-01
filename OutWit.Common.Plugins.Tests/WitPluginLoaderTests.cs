using NUnit.Framework.Legacy;
using OutWit.Common.Plugins.Tests.Mock;
using OutWit.Common.Plugins.Tests.Mock.CircularA;
using OutWit.Common.Plugins.Tests.Mock.CircularB;
using OutWit.Common.Plugins.Tests.Mock.DuplicateNamePlugin;
using OutWit.Common.Plugins.Tests.Mock.Interfaces;
using OutWit.Common.Plugins.Tests.Mock.MissingDependencyPlugin;
using OutWit.Common.Plugins.Tests.Mock.PluginA;
using OutWit.Common.Plugins.Tests.Mock.PluginB;
using OutWit.Common.Plugins.Tests.Mock.VersionMismatchPlugin;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OutWit.Common.Plugins.Tests
{
    [TestFixture]
    public class WitPluginLoaderTests
    {
        private string _testRoot;
        private string _pluginDir;
        
        [SetUp]
        public void Setup()
        {
            // Create a unique root directory for each test run to ensure isolation.
            _testRoot = Path.Combine(Path.GetTempPath(), "PluginLoaderTests", Guid.NewGuid().ToString());
            _pluginDir = Path.Combine(_testRoot, "modules");
            Directory.CreateDirectory(_pluginDir);
        }

        [TearDown]
        public void Teardown()
        {
            //Clean up the directory after each test.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, recursive: true);
            }

        }

        // Helper to copy a plugin assembly and its .deps.json to the test directory.
        private void StagePlugin(Type pluginType)
        {
            var assembly = pluginType.Assembly;
            var sourcePath = assembly.Location;
            var depsPath = sourcePath.Replace(".dll", ".deps.json");
            var targetPath = Path.Combine(_pluginDir, Path.GetFileName(sourcePath));

            File.Copy(sourcePath, targetPath, true);
            if (File.Exists(depsPath))
            {
                File.Copy(depsPath, targetPath.Replace(".dll", ".deps.json"), true);
            }
        }

        [Test]
        public void LoadAllWithValidPluginsSortsCorrectlyTest()
        {
            // Arrange: Stage plugins with dependencies and priorities.
            // PluginB (Priority 100) depends on PluginA (Priority 200).
            StagePlugin(typeof(PluginA));
            StagePlugin(typeof(PluginB));

            var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: false);

            // Act
            loader.Load();
            var loadedPlugins = loader.ToList();

            // Assert
            Assert.That(loadedPlugins, Has.Count.EqualTo(2));
            // Despite PluginB having a higher priority (100 vs 200),
            // it must be loaded after its dependency, PluginA.
            Assert.That(loadedPlugins[0], Is.TypeOf<PluginA>());
            Assert.That(loadedPlugins[1], Is.TypeOf<PluginB>());
        }

        [Test]
        public void LoadAllFailsWithMissingDependencyTest()
        {
            // Arrange: Stage a plugin but not its dependency.
            StagePlugin(typeof(MissingDependencyPlugin));

            var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: false);

            // Act & Assert
            var ex = Assert.Throws<AggregateException>(() => loader.Load());
            Assert.That(ex.InnerExceptions, Has.Count.EqualTo(1));
            StringAssert.Contains("unresolved dependency on 'NonExistentPlugin'", ex.InnerExceptions[0].Message);
        }

        [Test]
        public void LoadAllFailsWithVersionMismatchTest()
        {
            // Arrange: Stage PluginA v1.0.0 and a plugin that requires v2.0.0.
            StagePlugin(typeof(PluginA));
            StagePlugin(typeof(VersionMismatchPlugin));

            var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: false);

            // Act & Assert
            var ex = Assert.Throws<AggregateException>(() => loader.Load());
            StringAssert.Contains("requires version '2.0.0' of 'PluginA', but found version '1.0.0'", ex.InnerExceptions[0].Message);
        }

        [Test]
        public void LoadAllFailsWithCircularDependencyTest()
        {
            // Arrange: Stage two plugins that depend on each other.
            StagePlugin(typeof(CircularA));
            StagePlugin(typeof(CircularB));

            var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: false);

            // Act & Assert
            var ex = Assert.Throws<AggregateException>(() => loader.Load());
            StringAssert.Contains("Circular dependency detected", ex.InnerExceptions[0].Message);
        }

        [Test]
        public void DiscoveryFailsWithDuplicatePluginNameTest()
        {
            // Arrange: Stage two different plugins that use the same manifest name.
            StagePlugin(typeof(PluginA));
            StagePlugin(typeof(DuplicateNamePlugin));

            var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: false);

            // Act & Assert
            var ex = Assert.Throws<AggregateException>(() => loader.Load());
            StringAssert.Contains("Duplicate plugin name 'PluginA' found", ex.InnerExceptions[0].Message);
        }

        [Test]
        public void UnloadPluginSucceedsWhenNotADependencyTest()
        {
            // Arrange
            StagePlugin(typeof(PluginA));
            StagePlugin(typeof(PluginB));
            using var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: true);
            loader.Load();

            // Act
            loader.UnloadPlugin("PluginB");
            
            // Assert
            Assert.That(loader.Count(), Is.EqualTo(1));
            Assert.That(loader.Single().GetType().AssemblyQualifiedName, Is.EqualTo(typeof(PluginA).AssemblyQualifiedName));
        
        }

        [Test]
        public void UnloadPluginFailsWhenItIsADependencyTest()
        {
            // Arrange
            StagePlugin(typeof(PluginA));
            StagePlugin(typeof(PluginB));
            using var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: true);
            loader.Load();
            try
            {
                loader.UnloadPlugin("PluginA");
            }
            catch (InvalidOperationException e)
            {
                StringAssert.Contains("Cannot unload plugin 'PluginA' because it is a dependency for: PluginB", e.Message);
            }
            
        }

        [Test]
        public void NonIsolatedModeResolvesTypesCorrectlyTest()
        {
            // Arrange: In non-isolated mode, type casting should work directly.
            StagePlugin(typeof(PluginA));
            var loader = new WitPluginLoader<ITestPlugin>(_pluginDir, useIsolatedContexts: false);

            // Act
            loader.Load();
            var plugin = loader.Single();
            
            loader.Dispose();

            // Assert
            // This cast would fail if contexts were isolated.
            Assert.That(plugin, Is.Not.Null);
            Assert.DoesNotThrow(() => { var casted = (PluginA)plugin; });
        }
    }
}