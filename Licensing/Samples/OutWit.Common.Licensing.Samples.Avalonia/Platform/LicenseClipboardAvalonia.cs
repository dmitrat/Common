using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using OutWit.Common.Licensing.MVVM.Platform;

namespace OutWit.Common.Licensing.Samples.Avalonia.Platform;

/// <summary>
/// The clipboard seam, on Avalonia. Ten lines, which is the number the design
/// claimed each adapter would cost — worth having the harness prove rather than
/// assert.
/// </summary>
internal sealed class LicenseClipboardAvalonia : ILicenseClipboard
{
    #region ILicenseClipboard

    public async Task SetTextAsync(string text)
    {
        var clipboard = TopLevel.GetTopLevel(MainWindow())?.Clipboard;

        if (clipboard != null)
            await clipboard.SetTextAsync(text);
    }

    #endregion

    #region Tools

    private static Window? MainWindow()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    #endregion
}
