using HarmonyLib;
using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UniverseLib;
using UniverseLib.UI;
using System;
using System.Reflection;
using DeveloperConsole.UI;
using DeveloperConsole.Patches;

namespace DeveloperConsole
{
    public class DeveloperConsole : ModBehaviour
    {
        public static DeveloperConsole Instance;
        internal static ConsoleManagerInstance Manager { get; private set; }

        public static UIBase uiBase { get; private set; }

        private ConsolePanel _consolePanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        public override object GetApi()
        {
            ModHelper.Console.WriteLine("Querying API");

            Manager ??= new ConsoleManagerInstance();
            return Manager;
        }

        private void Start()
        {
            Universe.Init(1f, CreateUI, (string message, LogType type) =>
                ModHelper.Console.WriteLine(message, type switch
                {
                    LogType.Log => MessageType.Message,
                    LogType.Warning => MessageType.Warning,
                    LogType.Error => MessageType.Error,
                    LogType.Assert => MessageType.Error,
                    LogType.Exception => MessageType.Error,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                }), default);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void Update()
        {
            _consolePanel?.ProcessInput();
        }

        private void CreateUI()
        {

            uiBase = UniversalUI.RegisterUI("Smaed.DeveloperConsole", null);
            uiBase.Canvas.sortingLayerID = 1;

            _consolePanel = new(uiBase);
            _consolePanel.Enabled = false;

            uiBase.Enabled = false;

            InputPatch.InConsole = false;
        }
    }
}
