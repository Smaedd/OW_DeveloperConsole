using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperConsole.ConsoleTypes
{
    internal struct Log
    {
        public Log(string message, ConsoleLogType type) 
        {
            Message = message;
            Type = type;
        }

        public string Message;
        public ConsoleLogType Type;

    }
}
