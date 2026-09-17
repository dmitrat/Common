namespace OutWit.Common.Fat
{
    /// <summary>
    /// What went wrong, for a <see cref="FatException"/>.
    /// </summary>
    public enum FatErrorKind
    {
        /// <summary>
        /// The device holds no FAT or exFAT volume where one was expected.
        /// </summary>
        NotRecognized,

        /// <summary>
        /// The on-disk structures contradict themselves or the device.
        /// </summary>
        Corrupt,

        /// <summary>
        /// The structures are valid but use something this library does not handle.
        /// </summary>
        Unsupported,

        /// <summary>
        /// The volume's sector size differs from the device's.
        /// </summary>
        SectorSizeMismatch,

        /// <summary>
        /// Nothing exists at the path.
        /// </summary>
        NotFound,

        /// <summary>
        /// The path names a file where a directory is needed.
        /// </summary>
        NotADirectory,

        /// <summary>
        /// The path names a directory where a file is needed.
        /// </summary>
        NotAFile,

        /// <summary>
        /// Something already exists at the path.
        /// </summary>
        AlreadyExists,

        /// <summary>
        /// The directory still has entries.
        /// </summary>
        NotEmpty,

        /// <summary>
        /// The volume, or the fixed root directory, or a directory at its size limit, has no room.
        /// </summary>
        NoSpace,

        /// <summary>
        /// The entry is marked read-only.
        /// </summary>
        AccessDenied,

        /// <summary>
        /// The file is open in a way that rules the operation out.
        /// </summary>
        InUse,

        /// <summary>
        /// The file would grow past what the volume can record: 4 GiB on FAT12/16/32, the
        /// volume's size on exFAT.
        /// </summary>
        TooLarge
    }
}
