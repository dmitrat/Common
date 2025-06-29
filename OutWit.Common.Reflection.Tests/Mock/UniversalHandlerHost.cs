using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Reflection.Tests.Mock
{
    internal class UniversalHandlerHost
    {
        public static object? CapturedSender { get; private set; }
        public static string? CapturedEventName { get; private set; }
        public static object[]? CapturedParameters { get; private set; }

        public static void Reset()
        {
            CapturedSender = null;
            CapturedEventName = null;
            CapturedParameters = null;
        }

        // The actual universal handler that matches the required delegate signature
        public static void Handle<T>(T sender, string eventName, object[] parameters) where T : class
        {
            CapturedSender = sender;
            CapturedEventName = eventName;
            CapturedParameters = parameters;
        }
    }
}
