using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.MVVM.Navigation.Modules.Tests.Mock;
using OutWit.Common.MVVM.Navigation.Modules.Utils;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Modules.Tests
{
    [TestFixture]
    public class UiModulesTests
    {
        #region Fields

        private ModuleCallLog m_log = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_log = new ModuleCallLog();
        }

        #region Phase Tests

        [Test]
        public async Task CompiledInModuleRunsBothPhasesTest()
        {
            var module = new SummaryModule(m_log);
            using var provider = Build(o => o.AddModule(module));

            Assert.That(m_log.Entries, Is.EqualTo(new[] { "Summary.Initialize" }));
            Assert.That(provider.GetRequiredService<SummaryService>(), Is.Not.Null);

            await provider.GetRequiredService<UiModules>().InitializeAsync(provider);

            Assert.That(m_log.Entries, Is.EqualTo(new[] { "Summary.Initialize", "Summary.OnInitialized" }));
            Assert.That(module.Context, Is.Not.Null);
            Assert.That(module.Context!.Services, Is.SameAs(provider));
        }

        [Test]
        public async Task ContextGivesRegistriesAndTheyAreFilledTest()
        {
            using var provider = Build(o => o.AddModule<SummaryModule>());
            await provider.GetRequiredService<UiModules>().InitializeAsync(provider);

            var routes = provider.GetRequiredService<IRouteRegistry>();
            var views = provider.GetRequiredService<IViewRegistry>();
            var contributions = provider.GetRequiredService<IContributionRegistry>();

            Assert.That(routes.TryGet(SummaryModule.ROUTE, out var route), Is.True);
            Assert.That(route!.ViewModelType, Is.EqualTo(typeof(SummaryViewModel)));
            Assert.That(route.Metadata, Is.EqualTo("Feature.Summary"));
            Assert.That(views.Contains(typeof(SummaryViewModel)), Is.True);
            Assert.That(contributions.Zone(SummaryModule.ZONE).Items, Has.Count.EqualTo(1));
            Assert.That(provider.ValidateNavigation(), Is.Empty);
        }

        [Test]
        public async Task ModuleRouteNavigatesWithModuleServiceInjectedTest()
        {
            using var provider = Build(o => o.AddModule<SummaryModule>());
            await provider.GetRequiredService<UiModules>().InitializeAsync(provider);
            var navigation = provider.GetRequiredService<INavigationService>();

            var result = await navigation.NavigateAsync(SummaryModule.ROUTE);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(navigation.Outlet().Content, Is.InstanceOf<SummaryViewModel>());
            Assert.That(((SummaryViewModel)navigation.Outlet().Content!).Service, Is.SameAs(provider.GetRequiredService<SummaryService>()));
        }

        [Test]
        public async Task ModulesRunInRegistrationOrderTest()
        {
            using var provider = Build(o => o.AddModule(new SecondModule(m_log)).AddModule(new SummaryModule(m_log)));
            await provider.GetRequiredService<UiModules>().InitializeAsync(provider);

            Assert.That(m_log.Entries, Is.EqualTo(new[]
            {
                "Second.Initialize",
                "Summary.Initialize",
                "Second.OnInitialized",
                "Summary.OnInitialized"
            }));
            Assert.That(provider.GetRequiredService<UiModules>().Names, Is.EqualTo(new[] { nameof(SecondModule), nameof(SummaryModule) }));
        }

        [Test]
        public async Task InitializeAsyncIsIdempotentTest()
        {
            using var provider = Build(o => o.AddModule(new SummaryModule(m_log)));
            var modules = provider.GetRequiredService<UiModules>();

            await modules.InitializeAsync(provider);
            await modules.InitializeAsync(provider);

            Assert.That(m_log.Entries.Count(entry => entry == "Summary.OnInitialized"), Is.EqualTo(1));
        }

        #endregion

        #region Failure Tests

        [Test]
        public async Task ModuleFailingInPhaseTwoDoesNotStopOthersTest()
        {
            using var provider = Build(o => o.AddModule(new BrokenModule(m_log, throwInInitialize: false)).AddModule(new SecondModule(m_log)));
            var modules = provider.GetRequiredService<UiModules>();

            await modules.InitializeAsync(provider);

            Assert.That(m_log.Entries, Does.Contain("Second.OnInitialized"));
            Assert.That(modules.Failures, Has.Count.EqualTo(1));
            Assert.That(modules.Failures[0].Module, Is.EqualTo(nameof(BrokenModule)));
            Assert.That(modules.Failures[0].Phase, Is.EqualTo("OnInitialized"));
            Assert.That(modules.Failures[0].Error, Is.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public async Task ModuleFailingInPhaseOneIsSkippedInPhaseTwoTest()
        {
            using var provider = Build(o => o.AddModule(new BrokenModule(m_log, throwInInitialize: true)).AddModule(new SecondModule(m_log)));
            var modules = provider.GetRequiredService<UiModules>();

            await modules.InitializeAsync(provider);

            Assert.That(m_log.Entries, Is.EqualTo(new[] { "Broken.Initialize", "Second.Initialize", "Second.OnInitialized" }));
            Assert.That(modules.Failures, Has.Count.EqualTo(1));
            Assert.That(modules.Failures[0].Phase, Is.EqualTo("Initialize"));
        }

        #endregion

        #region Folder Tests

        [Test]
        public async Task EmptyFolderLoadsNothingTest()
        {
            var folder = Path.Combine(Path.GetTempPath(), "OutWit.Navigation.Modules.Tests", Guid.NewGuid().ToString("N"));

            try
            {
                using var provider = Build(o =>
                {
                    o.ScanFolder = true;
                    o.Folder = folder;
                    o.AddModule(new SummaryModule(m_log));
                });
                var modules = provider.GetRequiredService<UiModules>();
                await modules.InitializeAsync(provider);

                Assert.That(Directory.Exists(folder), Is.True);
                Assert.That(modules.Modules, Has.Count.EqualTo(1));
                Assert.That(modules.Failures, Is.Empty);
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, recursive: true);
            }
        }

        [Test]
        public void DisabledScanDoesNotTouchTheFolderTest()
        {
            var folder = Path.Combine(Path.GetTempPath(), "OutWit.Navigation.Modules.Tests", Guid.NewGuid().ToString("N"));

            using var provider = Build(o =>
            {
                o.Folder = folder;
                o.ScanFolder = false;
            });

            Assert.That(Directory.Exists(folder), Is.False);
            Assert.That(provider.GetRequiredService<UiModules>().Modules, Is.Empty);
        }

        #endregion

        #region Guard Tests

        [Test]
        public void RegisterServicesTwiceThrowsTest()
        {
            var modules = new UiModules(new UiModulesOptions { ScanFolder = false });
            var services = new ServiceCollection();

            modules.RegisterServices(services);

            Assert.That(() => modules.RegisterServices(services), Throws.InvalidOperationException);
        }

        [Test]
        public void InitializeBeforeRegisterThrowsTest()
        {
            var modules = new UiModules(new UiModulesOptions { ScanFolder = false });

            Assert.That(() => modules.InitializeAsync(new ServiceCollection().BuildServiceProvider()), Throws.InvalidOperationException);
        }

        #endregion

        #region Tools

        private static ServiceProvider Build(Action<UiModulesOptions> configure)
        {
            var services = new ServiceCollection();

            services.AddNavigation();
            services.AddUiModules(o =>
            {
                o.ScanFolder = false;
                configure(o);
            });

            return services.BuildServiceProvider();
        }

        #endregion
    }
}
