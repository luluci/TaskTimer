using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskTimer
{
    class Logger
    {
        private string rootDir;
        private string logDir;

        public Logger()
        {
            rootDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            logDir = rootDir + @"\log";
        }
    }
}
