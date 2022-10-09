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
    internal class ConVar
    {
        private FieldInfo _field;
        private PropertyInfo _property;

        public readonly Type Type;
        public string Info { get; set; } = null;

        public ConVar(FieldInfo field)
        {
            _field = field;
            _property = null;

            Type = field.FieldType;
        }

        public ConVar(PropertyInfo property)
        {
            _field = null;
            _property = property;

            Type = property.PropertyType;
        }

        public void SetValue(object val)
        {
            if (_field != null)
            {
                _field.SetValue(null, val);
            }
            else // property
            {
                _property.SetValue(null, val);
            }
        }

        public object GetValue()
        {
            if (_field != null)
            {
                return _field.GetValue(null);
            }
            else // property
            {
                return _property.GetValue(null);
            }
        }

        public object[] GetCustomAttributes(Type type, bool inherit = false)
        {
            if (_field != null)
            {
                return _field.GetCustomAttributes(type, inherit);
            }
            else // property
            {
                return _property.GetCustomAttributes(type, inherit);
            }
        }
    }

    internal class ConCommand
    {
        public readonly int RequiredArgs;
        public readonly int MaxArgs;

        public readonly ParameterInfo[] Parameters;

        public string Info { get; set; } = null;

        public readonly MethodInfo Method;

        public ConCommand(MethodInfo method)
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

    public class ConsoleManager : IConsoleManager
    {
        private static Dictionary<string, ConVar> _convars = null;
        private static Dictionary<string, ConCommand> _concommands = null;

        private const int MAX_CONSOLE_MESSAGES = 255;
        private static Queue<string> _conlog = null;

        public event EventHandler<string> LogChanged;

        private static void WriteLine(string message, MessageType type = MessageType.Message)
        {
            DeveloperConsole.Instance.ModHelper.Console.WriteLine(message, type);
        }

        public void Log(string message)
        {
            if (_conlog == null)
            {
                WriteLine($"ConsoleManager.Log called with \"{message}\" while uninitialized!", MessageType.Warning);
                return;
            }

            _conlog.Enqueue(message);
            
            if (_conlog.Count > MAX_CONSOLE_MESSAGES)
            {
                _conlog.Dequeue();
            }

            OnLogChanged();
        }

        private void OnLogChanged()
        {
            StringBuilder builder = new();

            foreach (var line in _conlog)
            {
                builder.AppendLine(line);
            }

            LogChanged?.Invoke(this, builder.ToString());
        }

        public void LoadAttributes(Assembly assembly, Type containerType, Type consoleType)
        {
            if ( !containerType.IsSubclassOf(typeof(Attribute)) ||
                 !consoleType.IsSubclassOf(typeof(Attribute)) )
            {
                WriteLine("LoadAttributes called with non-Attribute types!", MessageType.Error);
                return;
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
                WriteLine("LoadAttributes called with non-conforming consoleType!", MessageType.Error);
                return;
            }

            _convars ??= new();
            _concommands ??= new();
            _conlog ??= new();

            foreach (Type type in GetContainerTypes(assembly, containerType))
            {
                foreach (ConVar convar in GetConVars(type, consoleType))
                {
                    foreach (var console in convar.GetCustomAttributes(consoleType))
                    {
                        string name = (string)nameProp.GetValue(console);
                        convar.Info = (string)infoProp.GetValue(console);

                        _convars.TryAdd(name, convar);
                    }
                }

                foreach (ConCommand command in GetConCommands(type, consoleType))
                {
                    foreach (var console in command.GetCustomAttributes(consoleType))
                    {
                        string name = (string)nameProp.GetValue(console);
                        command.Info = (string)infoProp.GetValue(console);

                        _concommands.TryAdd(name, command);
                    }
                }
            }
        }

        public int GetStringValue(string name, out string value, bool silent = false) => (int)_GetStringValue(name, out value, silent);
        public int GetValue(string name, out object value, bool silent = false) => (int)_GetValue(name, out value, silent);
        public int SetValue(string name, object value, bool silent = false) => (int)_SetValue(name, value, silent);
        public int RunCommand(string name, object[] arguments, bool silent = false) => (int)_RunCommand(name, arguments, silent);

        private ValueResult _GetStringValue(string name, out string value, bool silent = false)
        {
            value = null;

            if (!_convars.TryGetValue(name, out var convar))
            {
                if (!silent)
                    Log($"Unknown variable: \"{name}\"");

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
                WriteLine($"Unable to convert {name} to string", MessageType.Error);
            }
            catch (Exception e)
            {
                WriteLine(e.StackTrace, MessageType.Error);
            }

            if (!silent)
                Log($"An error occurred in getting the value of: \"{name}\"");

            return ValueResult.InvalidValue;
        }

        private ValueResult _GetValue(string name, out object value, bool silent = false)
        {
            value = null;

            if (!_convars.TryGetValue(name, out var convar))
            {
                if (!silent)
                    Log($"Unknown variable: \"{name}\"");

                return ValueResult.UnknownValue;
            }

            try
            {
                value = convar.GetValue();
                return ValueResult.Success;
            }
            catch (Exception e)
            {
                WriteLine(e.StackTrace, MessageType.Error);
            }

            if (!silent)
                Log($"An error occurred in getting the value of: \"{name}\"");

            return ValueResult.InvalidValue;
        }

        private ValueResult _SetValue(string name, object value, bool silent = false)
        {
            if (!_convars.TryGetValue(name, out var convar))
            {
                if (!silent)
                    Log($"Unknown variable: \"{name}\"");

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
                WriteLine($"Unable to convert given value to {name}", MessageType.Error);
            }
            catch (Exception e)
            {
                WriteLine(e.StackTrace, MessageType.Error);
            }

            if (!silent)
                Log($"An error occurred in setting: \"{name}\"");

            return ValueResult.InvalidValue;
        }

        private RunCommandResult _RunCommand(string name, object[] arguments, bool silent = false)
        {
            if (!_concommands.TryGetValue(name, out var command))
            {
                if (!silent)
                    Log($"Invalid command \"{name}\"");

                return RunCommandResult.UnknownCommand;
            }

            try
            {
                int givenArgs = arguments.Length;

                if (givenArgs < command.RequiredArgs || givenArgs > command.MaxArgs)
                {
                    if (!silent)
                        Log($"Command \"{name}\" requires {command.RequiredArgs} <= # args <= {command.MaxArgs}. {givenArgs} arguments were given.");

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
                WriteLine($"Unable to convert the arguments for {name}", MessageType.Error);
            }
            catch (Exception e)
            {
                WriteLine(e.StackTrace);
            }

            if (!silent)
                Log($"An error occurred running the command \"{name}\"");

            return RunCommandResult.InvalidArgs;
        }

        private IEnumerable<Type> GetContainerTypes(Assembly assembly, Type containerType)
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

        private IEnumerable<ConVar> GetConVars(Type type, Type consoleType)
        {
            foreach(FieldInfo field in type.GetFields())
            {
                if (field.GetCustomAttributes(consoleType, false).Length > 0)
                {
                    if (field.IsLiteral || field.IsInitOnly)
                    {
                        WriteLine($"Console type used on immutable field {type.Name}");
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
                        WriteLine($"Console type used on immutable/inaccessible property {type.Name}");
                        continue;
                    }

                    yield return new(prop);
                }
            }
        }

        private IEnumerable<ConCommand> GetConCommands(Type type, Type consoleType)
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
