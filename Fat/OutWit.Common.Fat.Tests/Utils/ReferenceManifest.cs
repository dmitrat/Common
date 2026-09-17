namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Fixtures/manifest.json: what the Linux tools saw in each reference image.
    /// </summary>
    internal sealed class ReferenceManifest
    {
        #region Properties

        public int Format { get; init; }

        public string Generator { get; init; } = "";

        public Dictionary<string, string> Tools { get; init; } = new();

        public List<ReferenceImage> Images { get; init; } = new();

        #endregion
    }
}
