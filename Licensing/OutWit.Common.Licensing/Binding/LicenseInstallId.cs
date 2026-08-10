using System;
using System.IO;
using System.Security.Cryptography;

namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// The identity of an <b>installation</b> — 128 random bits, decided once
    /// when a deployment is created and never again.
    /// <para>
    /// It exists because a container has no stable hardware identity: inside one,
    /// the OS machine id is not preserved across recreation, so a
    /// hardware-bound server licence would die on an ordinary
    /// <c>docker compose up --force-recreate</c>. A random value the
    /// installation owns has none of that problem.
    /// </para>
    /// <para>
    /// It identifies an installation. <b>Not</b> a machine — the container can
    /// move hosts. Not a customer; that is the customer block. Not a tenant;
    /// that is a name.
    /// </para>
    /// <para>
    /// It is <b>not a secret</b>. It is an identifier, and the licence records
    /// only its hash — nothing breaks if an operator reads it. What matters is
    /// that it is unguessable, so a second deployment cannot invent the first
    /// one's identity; it would have to steal it.
    /// </para>
    /// </summary>
    public static class LicenseInstallId
    {
        #region Constants

        /// <summary>Where the generated form is kept, beside the licences.</summary>
        public const string FILE_NAME = "install-id";

        /// <summary>How many random bytes. 128 bits: unguessable, and short enough to paste.</summary>
        public const int BYTE_COUNT = 16;

        #endregion

        #region Functions

        /// <summary>
        /// The installation id for this deployment: the configured value when
        /// there is one, otherwise a generated value persisted beside the
        /// licences.
        /// <para>
        /// <b>Configuration wins, and the order is the whole point.</b> An
        /// installer writes <c>Licensing__InstallId</c> into <c>.env</c> at
        /// deploy time, which makes the identity available before the host
        /// starts, identical across replicas of one deployment, and knowable
        /// before first start — so a customer can request a licence while
        /// installing rather than after. The generated file is the fallback for
        /// the environments no installer touched: a hand-rolled
        /// <c>docker compose up</c>, a developer machine, a test rig.
        /// </para>
        /// </summary>
        /// <param name="configured">The value from configuration, if any.</param>
        /// <param name="directory">Where licences live; the fallback file goes beside them.</param>
        public static string Resolve(string? configured, string directory)
        {
            if (!string.IsNullOrWhiteSpace(configured))
                return configured!.Trim();

            return FromFile(directory);
        }

        /// <summary>
        /// Reads the persisted id, creating one on first call.
        /// <para>
        /// Returns an empty string when the directory cannot be written to. That
        /// is deliberate: an unwritable volume is a deployment fault, and a
        /// binding factor that silently changed on every start would be far
        /// worse than one that is absent — an absent factor fails the licence
        /// visibly, a shifting one fails it mysteriously.
        /// </para>
        /// </summary>
        public static string FromFile(string directory)
        {
            var path = Path.Combine(directory, FILE_NAME);

            var existing = Read(path);
            if (existing.Length > 0)
                return existing;

            try
            {
                Directory.CreateDirectory(directory);

                var generated = Generate();

                // Written through a temporary file and moved into place, so a
                // process killed mid-write cannot leave a half-written identity
                // that reads back as a different installation.
                var temporary = path + ".tmp";
                File.WriteAllText(temporary, generated);

                if (File.Exists(path))
                {
                    File.Delete(temporary);
                    return Read(path);
                }

                File.Move(temporary, path);

                Protect(path);

                return generated;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Lost a race with another replica starting at the same instant,
                // or the volume is read-only. Re-read before giving up: the
                // former leaves a perfectly good id on disk.
                return Read(path);
            }
        }

        /// <summary>A fresh identity — 128 random bits as lower-case hex.</summary>
        public static string Generate()
        {
            var bytes = new byte[BYTE_COUNT];

            using var random = RandomNumberGenerator.Create();
            random.GetBytes(bytes);

            return Hex(bytes);
        }

        #endregion

        #region Tools

        private static string Read(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return string.Empty;

                return File.ReadAllText(path).Trim();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return string.Empty;
            }
        }

        private static string Hex(byte[] bytes)
        {
            var characters = new char[bytes.Length * 2];

            for (var index = 0; index < bytes.Length; index++)
            {
                var value = bytes[index];

                characters[index * 2] = Digit(value >> 4);
                characters[index * 2 + 1] = Digit(value & 0xF);
            }

            return new string(characters);
        }

        private static char Digit(int value)
        {
            return (char)(value < 10 ? '0' + value : 'a' + (value - 10));
        }

        /// <summary>
        /// Tightens permissions where the platform has them. A nicety rather
        /// than a precondition — this is an identifier, not a credential.
        /// </summary>
        private static void Protect(string path)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
            }
        }

        #endregion
    }
}
