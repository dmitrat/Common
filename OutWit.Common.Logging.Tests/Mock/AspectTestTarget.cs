using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OutWit.Common.Logging.Aspects;

namespace OutWit.Common.Logging.Tests.Mock
{
    [Log]
    public class AspectTestTarget
    {
        public void SimpleMethod(int id, string name)
        {
            // This method will be logged
        }

        public void MethodThatThrows()
        {
            throw new ArgumentException("Test exception");
        }

        [NoLog]
        public void ExcludedMethod()
        {
            // This method will NOT be logged
        }

        [Measure]
        public void MeasuredMethod()
        {
            System.Threading.Thread.Sleep(10); // Simulate work
        }

        // Simulate an OnPropertyChanged method from MVVM
        public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // This call should be logged with special formatting
        }
    }

}
