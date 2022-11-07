using Newtonsoft.Json;
using OWML.ModHelper;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeveloperConsole.Input
{
    public static class BindManager
    {
        private static Dictionary<KeyCode, string> _consoleBinds = null;
        private const string Filename = "consolebinds.cfg";

        public static void Bind(KeyCode key, string command)
        {
            _consoleBinds[key] = command;

            // For now, serialise every time (THIS IS SUPER SLOW!!!)
            Serialize();
        }
        public static bool Bind(string keyCode, string command)
        {
            if (!Enum.TryParse(keyCode, true, out KeyCode key))
                return false;

            Bind(key, command);
            return true;
        }

        public static string GetBind(KeyCode key) => _consoleBinds[key];

        public static void ProcessInput()
        {
            if (_consoleBinds == null)
                return;

            // Slow but probably ok for now
            foreach (var item in _consoleBinds)
            {
                if (UniverseLib.Input.InputManager.GetKeyDown(item.Key))
                {
                    ConsoleManager.RunCommand(item.Value);
                }
            }
        }

        public static void Serialize()
        {
            DeveloperConsole.Instance.ModHelper.Storage.Save(_consoleBinds, Filename);
        }

        public static void Deserialize()
        {
            _consoleBinds = DeveloperConsole.Instance.ModHelper.Storage.Load<Dictionary<KeyCode, string>>(Filename);

            if (_consoleBinds == null)
            {
                DeveloperConsole.Instance.ModHelper.Console.WriteLine("Unable to deserialize console binds - creating a new object", OWML.Common.MessageType.Debug);
                _consoleBinds = new();
            }
        }
    }
}
