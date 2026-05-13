using Microsoft.Extensions.Logging;

namespace OutWit.Common.Logging.Interfaces
{
    public interface ILogManager
    {
        public ILoggerFactory LoggerFactory { get; }
    }
}
