using System;
using System.Reflection;

namespace DeveloperConsole
{
    public interface IConsoleManager
    {
        public bool LoadAttributes(Assembly assembly, Type containerType, Type consoleType);

        public int SetValue(string name, object value, bool silent = false);
        public int GetValue(string name, out object value, bool silent = false);
        public int GetStringValue(string name, out string value, bool silent = false);

        public int RunCommand(string name, object[] arguments, bool silent = false);

        public void Log(string message, int type = 0);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ConsoleContainer : Attribute { }

    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Method |
        AttributeTargets.Property,
        AllowMultiple = true
        )]
    public class ConsoleData : Attribute
    {
        public string Name { get; }
        public string Info { get; }
        public ConsoleData(string name, string info = null)
        {
            Name = name;
            Info = info;
        }
    }

    public enum ValueResult
    {
        Success = 0,
        UnknownValue,
        InvalidValue,
    }

    public enum RunCommandResult
    {
        Success = 0,
        UnknownCommand,
        InvalidArgCount,
        InvalidArgs,
    }

    public enum ConsoleLogType
    {
        Message = 0,
        Light,
        Warning,
        Error,
    }

    // Utility class to improve usability of console over OWML API, most of it has to be done this way
    // because of API limitations.
    public class ConsoleWrapper
    {
        private IConsoleManager _manager;

        public ConsoleWrapper(IConsoleManager manager)
        {
            _manager = manager;
        }

        public void Link(Assembly assembly)
        {
            _manager.LoadAttributes(assembly, typeof(ConsoleContainer), typeof(ConsoleData));
        }

        public ValueResult SetValue(string name, object value, bool silent = false) => (ValueResult)_manager.SetValue(name, value, silent);
        public ValueResult GetValue(string name, out object value, bool silent = false) => (ValueResult)_manager.GetValue(name, out value, silent);
        public ValueResult GetStringValue(string name, out string value, bool silent = false) => (ValueResult)_manager.GetStringValue(name, out value, silent);

        public RunCommandResult RunCommand(string name, object[] arguments, bool silent = false) => (RunCommandResult)_manager.RunCommand(name, arguments, silent);

        public void Log(string message, ConsoleLogType type = ConsoleLogType.Message) => _manager.Log(message, (int)type);
    }
}
