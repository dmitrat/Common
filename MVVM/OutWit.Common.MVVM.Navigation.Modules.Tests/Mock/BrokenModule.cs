using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.Plugins.Abstractions.Attributes;

namespace OutWit.Common.MVVM.Navigation.Modules.Tests.Mock
{
    /// <summary>
    /// Throws in whichever phase the test asks for.
    /// </summary>
    [WitPluginManifest("Broken")]
    public sealed class BrokenModule : UiModuleBase
    {
        #region Constructors

        public BrokenModule(ModuleCallLog log, bool throwInInitialize)
        {
            Log = log;
            ThrowInInitialize = throwInInitialize;
        }

        #endregion

        #region UiModuleBase

        public override void Initialize(IServiceCollection services)
        {
            Log.Entries.Add("Broken.Initialize");

            if (ThrowInInitialize)
                throw new InvalidOperationException("phase one failure");
        }

        protected override void OnInitialized(UiModuleContext context)
        {
            Log.Entries.Add("Broken.OnInitialized");
            throw new InvalidOperationException("phase two failure");
        }

        #endregion

        #region Properties

        public ModuleCallLog Log { get; }

        public bool ThrowInInitialize { get; }

        #endregion
    }
}
