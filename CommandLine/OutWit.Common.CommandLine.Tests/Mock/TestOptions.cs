using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.CommandLine.Tests.Mock
{
    // A helper class with options for use in tests.
    // It mimics a real-world command-line settings class.
    public class TestOptions
    {
        [Option('n', "name", Required = true, HelpText = "User name.")]
        public string? Name { get; set; }

        [Option('c', "count", Required = false, HelpText = "A certain count.")]
        public int Count { get; set; }

        [Option('v', "verbose", HelpText = "Enable verbose output.")]
        public bool Verbose { get; set; }

        [Option('t', "timeout", HelpText = "Timeout value.")]
        public TimeSpan? Timeout { get; set; }

        // This property lacks an Option attribute and should be ignored during serialization.
        public string IgnoredProperty { get; set; } = "should-be-ignored";
    }
}
