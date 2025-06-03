using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbFill
{
    public static class Logger
    {

        public static void Log(string message)
        {
            File.AppendAllText("logs.txt", message + Environment.NewLine);
        }
    }
}
