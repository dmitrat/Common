using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace OutWit.Common.Logging.Interfaces
{
    public interface ILogManager
    {
        public ILoggerFactory LoggerFactory { get; }
    }
}
