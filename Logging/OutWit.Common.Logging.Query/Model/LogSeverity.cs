using System.Collections.Generic;
using OutWit.Common.Enums;

namespace OutWit.Common.Logging.Query.Model
{
    public sealed class LogSeverity : StringEnum<LogSeverity>
    {
        #region Static Constants

        public static readonly LogSeverity Trace = new("Trace", 0);

        public static readonly LogSeverity Debug = new("Debug", 1);

        public static readonly LogSeverity Information = new("Information", 2);

        public static readonly LogSeverity Warning = new("Warning", 3);

        public static readonly LogSeverity Error = new("Error", 4);

        public static readonly LogSeverity Critical = new("Critical", 5);

        public static readonly LogSeverity Fatal = new("Fatal", 6);

        #endregion

        #region Constructors

        private LogSeverity(string value, int level) : base(value)
        {
            Level = level;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Returns all severity levels at or above the specified minimum level.
        /// </summary>
        /// <param name="minLevel">The minimum severity level.</param>
        public static IReadOnlyList<LogSeverity> LevelAtLeast(LogSeverity minLevel)
        {
            var levels = new List<LogSeverity>();
            foreach (var severity in GetAll())
            {
                if (severity.Level >= minLevel.Level)
                    levels.Add(severity);
            }
            return levels;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the numeric severity level (higher = more severe).
        /// </summary>
        public int Level { get; }

        #endregion
    }
}
