using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.MVVM.Utils
{
    public static class ProcessUtils
    {
        public static void OpenFile(string filePath)
        {
            var process = new Process
            {
                StartInfo = {FileName = "explorer", Arguments = "\"" + filePath + "\""}
            };

            process.Start();
        }
    }
}
