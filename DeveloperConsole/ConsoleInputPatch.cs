using HarmonyLib;

namespace DeveloperConsole
{
    [HarmonyPatch(typeof(BaseInputManager))]
    internal static class ConsoleInputPatch
    {
        public static bool InConsole = false;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BaseInputManager.IsInputMode))]
        public static void IsInputMode_Console(InputMode mask, ref bool __result)
        {
            if (InConsole)
                __result = (mask == InputMode.All) || ((mask & InputMode.None) == InputMode.None);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BaseInputManager.GetInputMode))]
        public static void GetInputMode_Console(ref InputMode __result)
        {
            if (InConsole)
                __result = InputMode.None;
        }
    }
}
