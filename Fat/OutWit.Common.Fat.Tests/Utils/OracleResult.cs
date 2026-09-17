namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// What <c>check_image.py</c> prints.
    /// </summary>
    internal sealed class OracleResult
    {
        #region Properties

        public List<string> Problems { get; init; } = new();

        public string Fsck { get; init; } = string.Empty;

        #endregion
    }
}
