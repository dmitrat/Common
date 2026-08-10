using OutWit.Common.Licensing.MVVM.Platform;
using OutWit.Common.MVVM.Interfaces;

namespace OutWit.Common.Licensing.MVVM.ViewModels
{
    /// <summary>
    /// The panel for a consumer that has no ApplicationViewModel to hang it off.
    /// <para>
    /// A service's licence page is a MudBlazor component and a test fixture is
    /// neither; making either invent a container class purely to satisfy a type
    /// parameter would be the tail wagging the dog. Everything else about it is
    /// the generic panel, unchanged.
    /// </para>
    /// </summary>
    public class LicensePanelViewModelStandalone : LicensePanelViewModel<object>
    {
        #region Constructors

        public LicensePanelViewModelStandalone(
            ILicenseGateway gateway,
            ILicenseClipboard? clipboard = null,
            ILicenseFileTransfer? files = null,
            IDispatcher? dispatcher = null)
            : base(new object(), gateway, clipboard, files, dispatcher)
        {
        }

        #endregion
    }
}
