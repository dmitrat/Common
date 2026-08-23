using System;

namespace OutWit.Common.MVVM.Navigation.Modules.Model
{
    /// <summary>
    /// A module that failed in one of its phases. One broken module does not stop the
    /// others; the failure is recorded here and logged.
    /// </summary>
    public sealed class UiModuleFailure
    {
        #region Constructors

        public UiModuleFailure(string module, string phase, Exception error)
        {
            Module = module;
            Phase = phase;
            Error = error;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Module} ({Phase}): {Error.Message}";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The module's manifest name, or its type name for a compiled-in module.
        /// </summary>
        public string Module { get; }

        /// <summary>
        /// "Initialize" or "OnInitialized".
        /// </summary>
        public string Phase { get; }

        /// <summary>
        /// What went wrong.
        /// </summary>
        public Exception Error { get; }

        #endregion
    }
}
