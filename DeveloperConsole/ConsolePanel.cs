using UniverseLib.UI;
using UniverseLib.UI.Panels;
using UniverseLib.UI.Models;
using UniverseLib.Config;
using UnityEngine;
using System.Linq;

namespace DeveloperConsole
{
    internal class ConsolePanel : PanelBase
    {
        public ConsolePanel(UIBase owner) : base(owner) {
            
        }

        public override string Name => "Developer Console";
        public override int MinWidth => 750;
        public override int MinHeight => 300;
        public override Vector2 DefaultAnchorMin => new(0.25f, 0.25f);
        public override Vector2 DefaultAnchorMax => new(0.75f, 0.75f);
        public override bool CanDragAndResize => true;

        private InputFieldRef InputField;
        public bool InputFocused => InputField?.Component?.isFocused ?? false;


        private bool _pressedEnter = false;

        private string _lastCommand = null;

        protected override void ConstructPanelContent()
        {
            UIFactory.CreatePanel("TextPanel", ContentRoot, out var panelContent, new Color(0.5f, 0.5f, 0.5f));

            // TODO: Scroll bar is MESSED up, doesn't show color correctly unless it is disabled and re-enabled
            var scrollObj = UIFactory.CreateScrollView(panelContent, "TextView", out var scrollContent, out var scrollBar, new Color(0.3f, 0.3f, 0.3f));
            UIFactory.SetLayoutElement(scrollObj, minHeight: 100, flexibleHeight: 500);

            var textView = UIFactory.CreateLabel(scrollContent, "ConsoleText", "", TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.9f), false, 16);
            UIFactory.SetLayoutElement(textView.gameObject, minHeight: 300, flexibleHeight: 99999);

            var inputRow = UIFactory.CreateHorizontalGroup(ContentRoot, "InputRow", false, false, true, true, 8, new Vector4(3, 3, 5, 5),
                default, TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(inputRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 99999);

            InputField = UIFactory.CreateInputField(inputRow, "CommandInput", "Enter Command...");
            UIFactory.SetLayoutElement(InputField.Component.gameObject, minHeight: 28, minWidth: 100, flexibleHeight: 0, flexibleWidth: 99999);

            InputField.Component.textComponent.fontSize = 16;
            InputField.PlaceholderText.fontSize = 16;

            DeveloperConsole.Manager.LogChanged += (object sender, string message) =>
            {
                textView.text = message;
                scrollBar.Slider.value = 1f;
            };

            var submitButton = UIFactory.CreateButton(inputRow, "SubmitButton", "Submit", new Color(0.33f, 0.5f, 0.33f));
            UIFactory.SetLayoutElement(submitButton.Component.gameObject, minHeight: 28, minWidth: 120, flexibleHeight: 0);
            submitButton.ButtonText.fontSize = 15;

            submitButton.OnClick += SubmitCommand;
        }

        public void ProcessInput()
        {
            if (InputFocused)
            {
                bool keysDown = UniverseLib.Input.InputManager.GetKey(KeyCode.Return) || UniverseLib.Input.InputManager.GetKey(KeyCode.KeypadEnter);

                if (!keysDown)
                {
                    _pressedEnter = false;
                }
                else if (!_pressedEnter)
                {
                    SubmitCommand();
                    _pressedEnter = true;
                }

                if (_lastCommand != null && UniverseLib.Input.InputManager.GetKey(KeyCode.UpArrow))
                {
                    InputField.Text = _lastCommand;
                    InputField.Component.caretPosition = InputField.Text.Length;
                }
            }
            else if (UniverseLib.Input.InputManager.GetKeyDown(KeyCode.BackQuote))
            {
                OnToggled();
            }
        }

        private void OnToggled()
        {
            Owner.Enabled = Enabled = !Owner.Enabled;
            ConsoleInputPatch.InConsole = Enabled;

            ConfigManager.Force_Unlock_Mouse = Enabled;                
        }

        protected override void OnClosePanelClicked()
        {
            OnToggled();
        }

        public void SubmitCommand()
        {
            void OnSuccess()
            {
                DeveloperConsole.Manager.Log("> " + InputField.Text);
                _lastCommand = InputField.Text;
                InputField.Text = "";
            }

            string[] allArgs = InputField.Text.Trim().Split(' ');

            string name = allArgs.First();
            string[] args = allArgs.Skip(1).ToArray();

            if (args.Length == 0)
            {
                var getResult = (ValueResult)DeveloperConsole.Manager.GetStringValue(name, out var value, true);
                if (getResult == ValueResult.Success)
                {
                    OnSuccess();
                    DeveloperConsole.Manager.Log($"{name} = {value}");
                    return;
                }

                // If we know the convar, we can leave
                if (getResult != ValueResult.UnknownValue)
                    return;
            }
            else if (args.Length == 1)
            {
                var setResult = (ValueResult)DeveloperConsole.Manager.SetValue(name, args[0], true);
                if (setResult == ValueResult.Success)
                {
                    OnSuccess();
                    return;
                }

                // If we know the convar, we can leave
                if (setResult != ValueResult.UnknownValue)
                    return;
            }

            var result = (RunCommandResult)DeveloperConsole.Manager.RunCommand(name, args, false);

            // Clear if valid command/args
            if (result == RunCommandResult.Success)
            {
                OnSuccess();
            } 
        }
    }
}
