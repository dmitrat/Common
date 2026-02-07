using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;
using System;
using System.Collections.Generic;
using System.Text;
using OutWit.Common.Attributes;
using OutWit.Common.MemoryPack.Attributes;

namespace OutWit.Common.NewRelic.Model
{
    /// <summary>
    /// Canonical log entry model used by OutWit services.
    /// Designed to be memory-pack friendly and WitRPC-friendly.
    /// </summary>
    [MemoryPackable]
    public partial class NewRelicLogEntry : ModelBase
    {
        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not NewRelicLogEntry entry)
                return false;

            return Timestamp.Is(entry.Timestamp)
                   && Level.Is(entry.Level)
                   && Message.Is(entry.Message)
                   && Exception.Is(entry.Exception)
                   && SourceContext.Is(entry.SourceContext)
                   && ServiceName.Is(entry.ServiceName)
                   && Host.Is(entry.Host)
                   && Environment.Is(entry.Environment)
                   && TraceId.Is(entry.TraceId)
                   && SpanId.Is(entry.SpanId);
        }

        public override NewRelicLogEntry Clone()
        {
            return new NewRelicLogEntry
            {
                Timestamp = Timestamp,
                Level = Level,
                Message = Message,
                Exception = Exception,
                SourceContext = SourceContext,
                ServiceName = ServiceName,
                Host = Host,
                Environment = Environment,
                TraceId = TraceId,
                SpanId = SpanId
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Log timestamp.
        /// </summary>
        [MemoryPackOrder(0)]
        [ToString]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Log level, e.g. "Information", "Warning", "Error".
        /// </summary>
        [MemoryPackOrder(1)]
        [ToString]
        [StringEnumFormatter<NewRelicLogSeverity>]
        public NewRelicLogSeverity? Level { get; set; }

        /// <summary>
        /// Log message rendered as plain text.
        /// </summary>
        [MemoryPackOrder(2)]
        [ToString]
        public string? Message { get; set; }

        /// <summary>
        /// Exception details if present (message + stack trace, etc.).
        /// </summary>
        [MemoryPackOrder(3)]
        public string? Exception { get; set; }

        /// <summary>
        /// Logger/source context (e.g. Serilog SourceContext or logger name).
        /// </summary>
        [MemoryPackOrder(4)]
        public string? SourceContext { get; set; }

        /// <summary>
        /// Logical service name (New Relic usually uses "service.name").
        /// </summary>
        [MemoryPackOrder(5)]
        public string? ServiceName { get; set; }

        /// <summary>
        /// Host / machine / container name.
        /// </summary>
        [MemoryPackOrder(6)]
        public string? Host { get; set; }

        /// <summary>
        /// Environment name (e.g. "dev", "staging", "prod").
        /// </summary>
        [MemoryPackOrder(7)]
        public string? Environment { get; set; }

        /// <summary>
        /// Distributed trace identifier, if available.
        /// </summary>
        [MemoryPackOrder(8)]
        public string? TraceId { get; set; }

        /// <summary>
        /// Span identifier within a distributed trace, if available.
        /// </summary>
        [MemoryPackOrder(9)]
        public string? SpanId { get; set; }

        #endregion
    }
}
