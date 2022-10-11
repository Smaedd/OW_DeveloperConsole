using DeveloperConsole.ConsoleTypes;
using OWML.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeveloperConsole
{
    // Wrapper class that is both instanceable and conforms to the requirements of the API.
    public class ConsoleManagerInstance : IConsoleManager
    {
        public bool LoadAttributes(Assembly assembly, Type containerType, Type consoleType) => ConsoleManager.LoadAttributes(assembly, containerType, consoleType);

        public int SetValue(string name, object value, bool silent = false) => (int)ConsoleManager.SetValue(name, value, silent);
        public int GetValue(string name, out object value, bool silent = false) => (int)ConsoleManager.GetValue(name, out value, silent);
        public int GetStringValue(string name, out string value, bool silent = false) => (int)ConsoleManager.GetStringValue(name, out value, silent);

        public int RunCommand(string name, object[] arguments, bool silent = false) => (int)ConsoleManager.RunCommand(name, arguments, silent);

        public void Log(string message, int type = 0) => ConsoleManager.Log(message, (ConsoleLogType)type);
    }

    internal static class ConsoleManager
    {
        private static Dictionary<string, Var> _convars = new();
        private static Dictionary<string, Command> _concommands = new();

        private const int MAX_CONSOLE_MESSAGES = 255;
        private static Queue<Log> _conlogs = new();
        public static int NumLogs => _conlogs.Count;
        public static event EventHandler LogChanged;

        public static bool LoadAttributes(Assembly assembly, Type containerType, Type consoleType)
        {
            if ( !containerType.IsSubclassOf(typeof(Attribute)) ||
                 !consoleType.IsSubclassOf(typeof(Attribute)) )
            {
                WriteModHelperLine("LoadAttributes called with non-Attribute types!", MessageType.Error);
                return false;
            }

            PropertyInfo nameProp = consoleType.GetProperty("Name");
            PropertyInfo infoProp = consoleType.GetProperty("Info");

            if ( nameProp == null || 
                !nameProp.CanRead || 
                nameProp.PropertyType != typeof(string) ||
                infoProp == null ||
                !infoProp.CanRead ||
                infoProp.PropertyType != typeof(string))
            {
                WriteModHelperLine("LoadAttributes called with non-conforming consoleType!", MessageType.Error);
                return false;
            }

            foreach (Type type in GetContainerTypes(assembly, containerType))
            {
                foreach (Var convar in GetConVars(type, consoleType))
                {
                    foreach (var console in convar.GetCustomAttributes(consoleType))
                    {
                        string name = (string)nameProp.GetValue(console);
                        convar.Info = (string)infoProp.GetValue(console);

                        _convars.TryAdd(name, convar);
                    }
                }

                foreach (Command command in GetConCommands(type, consoleType))
                {
                    foreach (var console in command.GetCustomAttributes(consoleType))
                    {
                        string name = (string)nameProp.GetValue(console);
                        command.Info = (string)infoProp.GetValue(console);

                        _concommands.TryAdd(name, command);
                    }
                }
            }

            return true;
        }

        public static ValueResult GetStringValue(string name, out string value, bool silent = false)
        {
            value = null;

            if (!_convars.TryGetValue(name, out var convar))
            {
                if (!silent)
                    Log($"Unknown variable: \"{name}\"", ConsoleLogType.Error);

                return ValueResult.UnknownValue;
            }

            try
            {
                var real_value = convar.GetValue();
                if (real_value is string)
                {
                    value = (string)real_value;
                    return ValueResult.Success;
                }

                value = TypeDescriptor.GetConverter(real_value.GetType()).ConvertToString(real_value);
                return ValueResult.Success;
            }
            catch (NotSupportedException)
            {
                WriteModHelperLine($"Unable to convert {name} to string", MessageType.Error);
            }
            catch (Exception e)
            {
                WriteModHelperLine(e.StackTrace, MessageType.Error);
            }

            if (!silent)
                Log($"An error occurred in getting the value of: \"{name}\"", ConsoleLogType.Error);

            return ValueResult.InvalidValue;
        }

        public static ValueResult GetValue(string name, out object value, bool silent = false)
        {
            value = null;

            if (!_convars.TryGetValue(name, out var convar))
            {
                if (!silent)
                    Log($"Unknown variable: \"{name}\"", ConsoleLogType.Error);

                return ValueResult.UnknownValue;
            }

            try
            {
                value = convar.GetValue();
                return ValueResult.Success;
            }
            catch (Exception e)
            {
                WriteModHelperLine(e.StackTrace, MessageType.Error);
            }

            if (!silent)
                Log($"An error occurred in getting the value of: \"{name}\"", ConsoleLogType.Error);

            return ValueResult.InvalidValue;
        }

        public static ValueResult SetValue(string name, object value, bool silent = false)
        {
            if (!_convars.TryGetValue(name, out var convar))
            {
                if (!silent)
                    Log($"Unknown variable: \"{name}\"", ConsoleLogType.Error);

                return ValueResult.UnknownValue;
            }

            try
            {
                if (convar.Type == value.GetType())
                {
                    convar.SetValue(value);
                    return ValueResult.Success;
                }

                var type_val = TypeDescriptor.GetConverter(convar.Type).ConvertFrom(value);
                convar.SetValue(type_val);

                return ValueResult.Success;
            }
            catch (NotSupportedException)
            {
                WriteModHelperLine($"Unable to convert given value to {name}", MessageType.Error);
            }
            catch (Exception e)
            {
                WriteModHelperLine(e.StackTrace, MessageType.Error);
            }

            if (!silent)
                Log($"An error occurred in setting: \"{name}\"", ConsoleLogType.Error);

            return ValueResult.InvalidValue;
        }

        public static RunCommandResult RunCommand(string name, object[] arguments, bool silent = false)
        {
            if (!_concommands.TryGetValue(name, out var command))
            {
                if (!silent)
                    Log($"Invalid command \"{name}\"", ConsoleLogType.Error);

                return RunCommandResult.UnknownCommand;
            }

            try
            {
                int givenArgs = arguments.Length;

                if (givenArgs < command.RequiredArgs || givenArgs > command.MaxArgs)
                {
                    if (!silent)
                        Log($"Command \"{name}\" requires {command.RequiredArgs} <= # args <= {command.MaxArgs}. {givenArgs} arguments were given.", ConsoleLogType.Error);

                    return RunCommandResult.InvalidArgCount;
                }

                var paramsAndInfo = arguments.Zip(command.Parameters, (p, i) => new { Param = p, Info = i });

                object[] type_args = new object[command.MaxArgs];
                int num_created = 0;

                // Go through  and attempt to convert parameters to the correct type
                foreach (var item in paramsAndInfo)
                {
                    if (item.Info.ParameterType == item.Param.GetType())
                    {
                        type_args[num_created++] = item.Param;
                        continue;
                    }

                    var type_val = TypeDescriptor.GetConverter(item.Info.ParameterType).ConvertFrom(item.Param);
                    type_args[num_created++] = type_val;
                }

                // Default to optional
                for (int i = num_created; i < command.MaxArgs; ++i)
                {
                    type_args[i] = Type.Missing;
                }

                // Call allowing optional params
                command.Method.Invoke(null,
                                      BindingFlags.OptionalParamBinding |
                                      BindingFlags.InvokeMethod,
                                      null,
                                      type_args,
                                      CultureInfo.InvariantCulture
                                      );

                return RunCommandResult.Success;
            }
            catch (NotSupportedException)
            {
                WriteModHelperLine($"Unable to convert the arguments for {name}", MessageType.Error);
            }
            catch (Exception e)
            {
                WriteModHelperLine(e.StackTrace);
            }

            if (!silent)
                Log($"An error occurred running the command \"{name}\"", ConsoleLogType.Error);

            return RunCommandResult.InvalidArgs;
        }

        private static void WriteModHelperLine(string message, MessageType type = MessageType.Message)
        {
            DeveloperConsole.Instance.ModHelper.Console.WriteLine(message, type);
        }

        public static void Log(string message, ConsoleLogType type = ConsoleLogType.Message)
        {
            if (_conlogs == null)
            {
                WriteModHelperLine($"ConsoleManager.Log called with \"{message}\" while uninitialized!", MessageType.Warning);
                return;
            }

            _conlogs.Enqueue( new(message, type) );

            // Delete excess
            for (int i = 0; i < _conlogs.Count - MAX_CONSOLE_MESSAGES; ++i)
            {
                _conlogs.Dequeue();
            }

            OnLogChanged();
        }

        public static Log GetLog(int index)
        {
            return _conlogs.ElementAt(index);
        }

        private static void OnLogChanged()
        {
            LogChanged?.Invoke(null, null);
        }

        private static IEnumerable<Type> GetContainerTypes(Assembly assembly, Type containerType)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(containerType, false).Length > 0)
                {
                    // Only allow on abstract+sealed (i.e. static) types, since we don't want non-static methods/properties
                    if ( !(type.IsAbstract && type.IsSealed) )
                        continue;

                    yield return type;
                }
            }
        }

        private static IEnumerable<Var> GetConVars(Type type, Type consoleType)
        {
            foreach(FieldInfo field in type.GetFields())
            {
                if (field.GetCustomAttributes(consoleType, false).Length > 0)
                {
                    if (field.IsLiteral || field.IsInitOnly)
                    {
                        WriteModHelperLine($"Console type used on immutable field {type.Name}");
                        continue;
                    }

                    yield return new(field);
                }
            }

            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.GetCustomAttributes(consoleType, false).Length > 0)
                {
                    if (!prop.CanWrite || !prop.CanRead)
                    {
                        WriteModHelperLine($"Console type used on immutable/inaccessible property {type.Name}");
                        continue;
                    }

                    yield return new(prop);
                }
            }
        }

        private static IEnumerable<Command> GetConCommands(Type type, Type consoleType)
        {
            foreach (MethodInfo method in type.GetMethods())
            {
                if (method.GetCustomAttributes(consoleType, false).Length > 0)
                {
                    yield return new(method);
                }
            }
        }
    }
}
