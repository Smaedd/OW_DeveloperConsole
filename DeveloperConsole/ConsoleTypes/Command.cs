using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperConsole.ConsoleTypes
{
    internal class Command
    {
        public readonly int RequiredArgs;
        public readonly int MaxArgs;

        public readonly ParameterInfo[] Parameters;

        public string Info { get; set; } = null;

        public readonly MethodInfo Method;

        public Command(MethodInfo method)
        {
            Method = method;

            Parameters = Method.GetParameters();

            RequiredArgs = Parameters.Where(p => !p.HasDefaultValue).Count();
            MaxArgs = Parameters.Count();
        }

        public object[] GetCustomAttributes(Type type, bool inherit = false)
        {
            return Method.GetCustomAttributes(type, inherit);
        }
    }
}
