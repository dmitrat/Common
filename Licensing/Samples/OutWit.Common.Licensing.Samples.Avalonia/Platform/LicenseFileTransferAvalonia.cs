using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using OutWit.Common.Licensing.MVVM.Platform;

namespace OutWit.Common.Licensing.Samples.Avalonia.Platform;

/// <summary>
/// The file seam, on Avalonia: a <c>.lic</c> in, a <c>.owlreq</c> out.
/// <para>
/// Both directions are paths the design has always described and nothing had
/// ever exercised — the request blob had never been written to a file, and no
/// verifier had ever read a licence in from one.
/// </para>
/// </summary>
internal sealed class LicenseFileTransferAvalonia : ILicenseFileTransfer
{
    #region Constants

    private const string LICENSE_EXTENSION = "lic";
    private const string REQUEST_EXTENSION = "owlreq";

    #endregion

    #region ILicenseFileTransfer

    public async Task<string?> OpenTextAsync()
    {
        var storage = Storage();
        if (storage == null)
            return null;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a licence",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Licence") { Patterns = new[] { $"*.{LICENSE_EXTENSION}" } },
                new("All files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count == 0)
            return null;

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    public async Task SaveTextAsync(string fileName, string content)
    {
        var storage = Storage();
        if (storage == null)
            return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the licence request",
            SuggestedFileName = fileName,
            DefaultExtension = REQUEST_EXTENSION
        });

        if (file == null)
            return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);

        await writer.WriteAsync(content);
    }

    #endregion

    #region Tools

    private static IStorageProvider? Storage()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        return TopLevel.GetTopLevel(window)?.StorageProvider;
    }

    #endregion
}
